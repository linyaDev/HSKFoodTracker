using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class MapComponent_FoodTracker : MapComponent
{
    private int tickCounter;
    private bool firstRun = true;
    private const int UpdateInterval = 2500;

    public float TotalNutrition;
    public float MealNutrition;
    public float RawNutrition;
    public float SpoilingIn2DaysNutrition;
    public float SpoilingIn5DaysNutrition;
    public float DailyConsumption;
    public float TotalDays;
    public float MealDays;
    public float RawDays;
    public List<PawnFoodInfo> PawnConsumptions = new List<PawnFoodInfo>();
    public List<FoodItemInfo> FoodItems = new List<FoodItemInfo>();
    public List<SpoilingFoodInfo> SpoilingFood = new List<SpoilingFoodInfo>();

    public MapComponent_FoodTracker(Map map) : base(map)
    {
    }

    private void AddPawnConsumption(Pawn pawn, PawnFoodCategory category)
    {
        if (pawn.needs?.food == null)
            return;

        float perTick = pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed);
        float perDay = perTick * 60000f;
        DailyConsumption += perDay;

        PawnConsumptions.Add(new PawnFoodInfo
        {
            pawnName = pawn.LabelShortCap,
            dailyNutrition = perDay,
            category = category
        });
    }

    public override void MapComponentTick()
    {
        if (firstRun)
        {
            firstRun = false;
            Recalculate();
            return;
        }
        tickCounter++;
        if (tickCounter < UpdateInterval)
            return;
        tickCounter = 0;

        Recalculate();
    }

    public void Recalculate()
    {
        TotalNutrition = 0f;
        MealNutrition = 0f;
        RawNutrition = 0f;
        DailyConsumption = 0f;
        PawnConsumptions.Clear();
        FoodItems.Clear();
        SpoilingFood.Clear();

        // Count food by category
        foreach (var kvp in map.resourceCounter.AllCountedAmounts)
        {
            var def = kvp.Key;
            if (!def.IsNutritionGivingIngestible || !def.ingestible.HumanEdible || kvp.Value <= 0)
                continue;

            float nutrition = def.GetStatValueAbstract(StatDefOf.Nutrition) * kvp.Value;

            bool isMeal = def.ingestible.preferability >= FoodPreferability.MealAwful
                          || (ThingDefOf.Pemmican != null && def == ThingDefOf.Pemmican)
                          || def.defName == "Pemmican";

            bool excluded = HSKFoodTrackerMod.Settings?.IsExcluded(def.defName) == true;
            if (!excluded)
            {
                TotalNutrition += nutrition;
                if (isMeal)
                    MealNutrition += nutrition;
                else
                    RawNutrition += nutrition;
            }

            FoodItems.Add(new FoodItemInfo
            {
                defName = def.defName,
                label = def.LabelCap,
                count = kvp.Value,
                nutrition = nutrition,
                isMeal = isMeal,
                excluded = excluded
            });
        }

        // Calculate per-pawn consumption
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.IsSlave) continue;
            if (pawn.IsQuestLodger())
                AddPawnConsumption(pawn, PawnFoodCategory.Guest);
            else
                AddPawnConsumption(pawn, PawnFoodCategory.Colonist);
        }

        foreach (var pawn in map.mapPawns.PrisonersOfColonySpawned)
            AddPawnConsumption(pawn, PawnFoodCategory.Prisoner);

        foreach (var pawn in map.mapPawns.SlavesOfColonySpawned)
            AddPawnConsumption(pawn, PawnFoodCategory.Slave);

        // Sort by category
        PawnConsumptions.Sort((a, b) => a.category.CompareTo(b.category));

        // Scan for spoiling food (within 5 days)
        const int fiveDaysTicks = 300000;
        foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
        {
            if (!thing.def.IsNutritionGivingIngestible || !thing.def.ingestible.HumanEdible)
                continue;

            // Skip corpses
            if (thing is Corpse)
                continue;

            // Only count food in stockpiles/storage
            if (!thing.IsInValidStorage())
                continue;

            var rottable = thing.TryGetComp<CompRottable>();
            if (rottable == null)
                continue;

            int ticksUntilRot = rottable.TicksUntilRotAtCurrentTemp;
            if (ticksUntilRot <= 0 || ticksUntilRot > fiveDaysTicks)
                continue;

            float daysLeft = ticksUntilRot / 60000f;
            SpoilingFood.Add(new SpoilingFoodInfo
            {
                defName = thing.def.defName,
                label = thing.LabelCapNoCount,
                count = thing.stackCount,
                daysLeft = daysLeft,
                nutrition = thing.GetStatValue(StatDefOf.Nutrition) * thing.stackCount
            });
        }
        SpoilingFood.Sort((a, b) => a.daysLeft.CompareTo(b.daysLeft));
        SpoilingIn2DaysNutrition = SpoilingFood.Where(f => f.daysLeft <= 2f).Sum(f => f.nutrition);
        SpoilingIn5DaysNutrition = SpoilingFood.Where(f => f.daysLeft > 2f && f.daysLeft <= 5f).Sum(f => f.nutrition);

        // Calculate days
        if (DailyConsumption > 0.001f)
        {
            TotalDays = TotalNutrition / DailyConsumption;
            MealDays = MealNutrition / DailyConsumption;
            RawDays = RawNutrition / DailyConsumption;
        }
        else
        {
            TotalDays = 999f;
            MealDays = 999f;
            RawDays = 999f;
        }
    }
}

public enum PawnFoodCategory : byte
{
    Colonist,
    Prisoner,
    Slave,
    Guest
}

public struct PawnFoodInfo
{
    public string pawnName;
    public float dailyNutrition;
    public PawnFoodCategory category;
}

public struct FoodItemInfo
{
    public string defName;
    public string label;
    public int count;
    public float nutrition;
    public bool isMeal;
    public bool excluded;
}

public struct SpoilingFoodInfo
{
    public string defName;
    public string label;
    public int count;
    public float daysLeft;
    public float nutrition;
}
