using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using System;

namespace SitAndFish
{
    public class ModEntry : Mod
    {
        private static IMonitor? SMonitor;
        private static bool isFishingWhileSitting = false;
        private static int sittingDirection = 2;

        // Oturma sprite frame indexleri (farmer_base sprite sheet)
        private const int SIT_DOWN = 107;
        private const int SIT_UP = 113;
        private const int SIT_SIDE = 117;

        // Olta atma animasyonunun oynamasına izin vermek için sayaç
        private static int castingGraceTicks = 0;
        private const int CASTING_GRACE_PERIOD = 30; // ~0.5 saniye animasyon süresini ver

        // Balık yakalama anı için
        private static bool wasNibble = false;

        public override void Entry(IModHelper helper)
        {
            SMonitor = this.Monitor;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.pressUseToolButton)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Prefix)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Postfix))
            );

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            SMonitor.Log("SitAndFish v1.2.0 yüklendi! Sandalyede otururken olta atabilirsiniz.", LogLevel.Info);
        }

        private static void PressUseToolButton_Prefix()
        {
            try
            {
                var farmer = Game1.player;
                if (farmer == null) return;

                if (farmer.isSitting.Value && farmer.CurrentTool is FishingRod)
                {
                    sittingDirection = farmer.FacingDirection;
                    isFishingWhileSitting = true;
                    castingGraceTicks = CASTING_GRACE_PERIOD; // cast animasyonuna izin ver
                    farmer.isSitting.Value = false;
                    SMonitor?.Log($"Oturarak olta atılıyor (yön: {sittingDirection})", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                SMonitor?.Log($"Prefix hata: {ex.Message}", LogLevel.Warn);
            }
        }

        private static void PressUseToolButton_Postfix()
        {
            try
            {
                if (isFishingWhileSitting)
                {
                    var farmer = Game1.player;
                    if (farmer != null)
                    {
                        farmer.isSitting.Value = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SMonitor?.Log($"Postfix hata: {ex.Message}", LogLevel.Warn);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!isFishingWhileSitting) return;

            var farmer = Game1.player;
            if (farmer == null) return;

            // Olta hâlâ kullanılıyor mu?
            if (!(farmer.CurrentTool is FishingRod rod))
            {
                ResetState();
                return;
            }

            bool fishingNow = farmer.UsingTool;

            if (!fishingNow)
            {
                // Balık tutma bitti, temizle
                ResetState();
                SMonitor?.Log("Balık tutma bitti, normal duruma dönüldü.", LogLevel.Debug);
                return;
            }

            // Olta atma animasyonu için grace period
            if (castingGraceTicks > 0)
            {
                castingGraceTicks--;
                return; // cast animasyonunun doğal oynamasına izin ver
            }

            // Balık yakalama/minigame anında sprite'ı zorlamayı durdur
            if (rod.isReeling || rod.pullingOutOfWater || rod.isFishing && rod.isNibbling)
            {
                // Minigame veya çekme anında doğal animasyonu kullan
                if (!wasNibble)
                {
                    SMonitor?.Log("Balık geldi! Doğal animasyon kullanılıyor.", LogLevel.Debug);
                    wasNibble = true;
                }
                return;
            }

            // Olta suda beklerken → oturma sprite'ını zorla
            if (rod.isFishing && !rod.isNibbling && !rod.isReeling)
            {
                wasNibble = false;
                ForceSittingSprite(farmer);
            }
        }

        private static void ForceSittingSprite(Farmer farmer)
        {
            int sittingFrame = sittingDirection switch
            {
                0 => SIT_UP,
                1 => SIT_SIDE,
                2 => SIT_DOWN,
                3 => SIT_SIDE,
                _ => SIT_DOWN
            };

            // Sol yöne bakıyorsa sprite'ı aynala
            bool flip = sittingDirection == 3;
            farmer.FarmerSprite.setCurrentSingleFrame(sittingFrame, 32000, false, flip);

            // Farmer offset — oturma pozisyonunda biraz aşağı kaydır (doğal görünsün)
            farmer.yOffset = 4f;
        }

        private static void ResetState()
        {
            isFishingWhileSitting = false;
            castingGraceTicks = 0;
            wasNibble = false;

            var farmer = Game1.player;
            if (farmer != null)
            {
                farmer.yOffset = 0f; // offset'i sıfırla
            }
        }
    }
}
