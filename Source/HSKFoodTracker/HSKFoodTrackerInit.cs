using Verse;

namespace HSKFoodTracker;

[StaticConstructorOnStartup]
public static class HSKFoodTrackerInit
{
    static HSKFoodTrackerInit()
    {
        Log.Message("[HSKFoodTracker] Loaded.");
    }
}
