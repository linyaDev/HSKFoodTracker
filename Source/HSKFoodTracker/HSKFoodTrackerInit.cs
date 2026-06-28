using HarmonyLib;
using Verse;

namespace HSKFoodTracker;

[StaticConstructorOnStartup]
public static class HSKFoodTrackerInit
{
    static HSKFoodTrackerInit()
    {
        new Harmony("HSKFoodTracker").PatchAll();
        Log.Message("[HSKFoodTracker] Loaded.");
    }
}
