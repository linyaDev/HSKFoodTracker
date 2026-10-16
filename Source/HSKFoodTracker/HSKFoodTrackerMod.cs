using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class HSKFoodTrackerSettings : ModSettings
{
    public float widgetX = -1f;
    public float widgetY = -1f;
    public List<string> excludedFoods = new List<string>();
    public bool showAnimalsInWidget = false;
    public bool showWidget = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref widgetX, "widgetX", -1f);
        Scribe_Values.Look(ref widgetY, "widgetY", -1f);
        Scribe_Collections.Look(ref excludedFoods, "excludedFoods", LookMode.Value);
        Scribe_Values.Look(ref showAnimalsInWidget, "showAnimalsInWidget", false);
        Scribe_Values.Look(ref showWidget, "showWidget", true);
        if (excludedFoods == null) excludedFoods = new List<string>();
        base.ExposeData();
    }

    public bool IsExcluded(string defName) => excludedFoods.Contains(defName);

    public void ToggleExcluded(string defName)
    {
        if (excludedFoods.Contains(defName))
            excludedFoods.Remove(defName);
        else
            excludedFoods.Add(defName);
        Write();
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
