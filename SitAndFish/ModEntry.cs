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

            SMonitor.Log("SitAndFish v1.3.0 yüklendi! Sandalyede otururken olta atabilirsiniz.", LogLevel.Info);
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

            bool fishingNow = farmer.UsingTool && farmer.CurrentTool is FishingRod;

            if (!fishingNow)
            {
                ResetState();
                SMonitor?.Log("Balık tutma bitti, normal duruma dönüldü.", LogLevel.Debug);
                return;
            }

            // Tüm süreç boyunca oturma sprite'ını zorla
            ForceSittingSprite(farmer);
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

            var farmer = Game1.player;
            if (farmer != null)
            {
                farmer.yOffset = 0f; // offset'i sıfırla
            }
        }
    }
}
