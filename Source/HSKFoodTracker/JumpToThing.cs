using RimWorld;
using Verse;

namespace HSKFoodTracker;

internal static class JumpToThing
{
    /// <summary>Jump camera to the first spawned thing of this def (current map first). True if found.</summary>
    public static bool TryJump(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        if (def == null)
            return false;

        var maps = Find.Maps;
        var current = Find.CurrentMap;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                bool isCurrent = map == current;
                if (pass == 0 ? !isCurrent : isCurrent)
                    continue;
                var things = map.listerThings.ThingsOfDef(def);
                if (things.Count > 0)
                {
                    CameraJumper.TryJumpAndSelect(things[0]);
                    return true;
                }
            }
        }
        return false;
    }
}
