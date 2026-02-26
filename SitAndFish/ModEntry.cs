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

        public override void Entry(IModHelper helper)
        {
            SMonitor = this.Monitor;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // Farmer.canMove veya Farmer.CanMove'u otururken true döndür (sadece olta için)
            // Oyun, oturuyorken tool kullanımını Farmer.pressUseToolButton() içinde kontrol ediyor
            // isSitting check'ini bypass etmek için pressUseToolButton'u patchleyeceğiz

            // Farmer.isSitting kontrolünü FishingRod için bypass et
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.pressUseToolButton)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Prefix)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PressUseToolButton_Postfix))
            );

            SMonitor.Log("SitAndFish yüklendi! Sandalyede otururken olta atabilirsiniz.", LogLevel.Info);
        }

        // Oturuyorken olta kullanmak isteniyorsa geçici olarak isSitting'i false yap
        private static bool wasSittingBeforeTool = false;

        private static void PressUseToolButton_Prefix()
        {
            try
            {
                var farmer = Game1.player;
                if (farmer == null) return;

                // Sadece olta tutuyorsa ve oturuyorsa bypass et
                if (farmer.isSitting.Value && farmer.CurrentTool is FishingRod)
                {
                    wasSittingBeforeTool = true;
                    farmer.isSitting.Value = false;
                    SMonitor?.Log("Oturma durumu geçici olarak devre dışı (olta atılıyor)", LogLevel.Debug);
                }
                else
                {
                    wasSittingBeforeTool = false;
                }
            }
            catch (Exception ex)
            {
                SMonitor?.Log($"Prefix hata: {ex.Message}", LogLevel.Warn);
                wasSittingBeforeTool = false;
            }
        }

        private static void PressUseToolButton_Postfix()
        {
            try
            {
                if (wasSittingBeforeTool)
                {
                    var farmer = Game1.player;
                    if (farmer != null)
                    {
                        farmer.isSitting.Value = true;
                        SMonitor?.Log("Oturma durumu geri yüklendi", LogLevel.Debug);
                    }
                    wasSittingBeforeTool = false;
                }
            }
            catch (Exception ex)
            {
                SMonitor?.Log($"Postfix hata: {ex.Message}", LogLevel.Warn);
                wasSittingBeforeTool = false;
            }
        }
    }
}
