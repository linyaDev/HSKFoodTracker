using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class Patch_PlaySettings
{
    private static Texture2D iconTexture;
    private static Texture2D Icon => iconTexture ??= ContentFinder<Texture2D>.Get("UI/FoodWidget", true);

    public static void Postfix(WidgetRow row, bool worldView)
    {
        if (worldView)
            return;

        var settings = HSKFoodTrackerMod.Settings;
        if (settings == null)
            return;

        row.ToggleableIcon(ref settings.showWidget, Icon,
            "FT_ToggleWidget".Translate(), SoundDefOf.Mouseover_ButtonToggle);
    }
}
