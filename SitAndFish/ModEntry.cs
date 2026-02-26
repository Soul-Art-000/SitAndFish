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
        private static int sittingDirection = 2; // default aşağı

        // Oturma sprite frame indexleri (SV farmer_base sprite sheet)
        private const int SIT_DOWN = 107;   // aşağı bakarak oturma
        private const int SIT_UP = 113;     // yukarı bakarak oturma
        private const int SIT_SIDE = 117;   // yan (sol/sağ) oturma

        public override void Entry(IModHelper helper)
        {
            SMonitor = this.Monitor;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // isSitting kontrolünü FishingRod için bypass et
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.pressUseToolButton)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Prefix)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Postfix))
            );

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderingWorld += OnRenderingWorld;

            SMonitor.Log("SitAndFish v1.1.0 yüklendi! Sandalyede otururken olta atabilirsiniz.", LogLevel.Info);
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

        // Her frame'de oturma sprite'ını zorla
        private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!isFishingWhileSitting) return;

            var farmer = Game1.player;
            if (farmer == null) return;

            bool fishingNow = farmer.UsingTool && farmer.CurrentTool is FishingRod;
            if (fishingNow)
            {
                ForceSittingSprite(farmer);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!isFishingWhileSitting) return;

            var farmer = Game1.player;
            if (farmer == null) return;

            bool fishingNow = farmer.UsingTool && farmer.CurrentTool is FishingRod;

            if (fishingNow)
            {
                // Oturma sprite'ını her tick'te zorla
                ForceSittingSprite(farmer);
            }
            else
            {
                // Balık tutma bitti
                isFishingWhileSitting = false;
                SMonitor?.Log("Olta bırakıldı, normal oturma durumuna dönüldü.", LogLevel.Debug);
            }
        }

        private static void ForceSittingSprite(Farmer farmer)
        {
            // Yöne göre doğru oturma frame'ini seç
            int sittingFrame = sittingDirection switch
            {
                0 => SIT_UP,     // yukarı
                1 => SIT_SIDE,   // sağ
                2 => SIT_DOWN,   // aşağı
                3 => SIT_SIDE,   // sol (aynalı)
                _ => SIT_DOWN
            };

            farmer.FarmerSprite.setCurrentSingleFrame(sittingFrame, 32000, false, sittingDirection == 3);
        }
    }
}
