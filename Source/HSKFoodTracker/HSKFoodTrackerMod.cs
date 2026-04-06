using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class HSKFoodTrackerSettings : ModSettings
{
    public float widgetX = -1f;
    public float widgetY = -1f;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref widgetX, "widgetX", -1f);
        Scribe_Values.Look(ref widgetY, "widgetY", -1f);
        base.ExposeData();
    }
}

public class HSKFoodTrackerMod : Mod
{
    public static HSKFoodTrackerSettings Settings;

    public HSKFoodTrackerMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<HSKFoodTrackerSettings>();
    }

    public override string SettingsCategory() => null;
}
