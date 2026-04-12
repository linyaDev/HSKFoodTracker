using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class Dialog_FoodDetails : Window
{
    private Vector2 scrollPosition;

    private static readonly Color GreenText = new Color(0.4f, 0.95f, 0.4f);
    private static readonly Color YellowText = new Color(0.95f, 0.95f, 0.4f);
    private static readonly Color RedText = new Color(0.95f, 0.4f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color RowBg = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    private static readonly Color SectionBg = new Color(0.15f, 0.15f, 0.15f, 0.8f);

    public override Vector2 InitialSize => new Vector2(520f, 600f);

    public Dialog_FoodDetails()
    {
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var tracker = Find.CurrentMap?.GetComponent<MapComponent_FoodTracker>();
        if (tracker == null)
            return;

        tracker.Recalculate();

        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "FT_FoodTitle".Translate());
        Text.Font = GameFont.Small;

        float y = 38f;

        // === Total days — big ===
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = GetDaysColor(tracker.TotalDays);
        string totalStr = tracker.TotalDays >= 999f ? "∞" : tracker.TotalDays.ToString("F1");
        Widgets.Label(new Rect(0f, y, inRect.width, 30f),
            "FT_TotalDays".Translate() + ": " + totalStr + " " + "FT_Days".Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 34f;

        // === Meals / Raw side by side ===
        float halfW = inRect.width / 2f;
        Text.Anchor = TextAnchor.MiddleCenter;

        GUI.color = GetDaysColor(tracker.MealDays);
        string mealStr = tracker.MealDays >= 999f ? "∞" : tracker.MealDays.ToString("F1");
        Widgets.Label(new Rect(0f, y, halfW, 22f),
            "FT_MealDays".Translate() + ": " + mealStr + " (" + tracker.MealNutrition.ToString("F0") + " " + "FT_Nutr".Translate() + ")");

        GUI.color = GetDaysColor(tracker.RawDays);
        string rawStr = tracker.RawDays >= 999f ? "∞" : tracker.RawDays.ToString("F1");
        Widgets.Label(new Rect(halfW, y, halfW, 22f),
            "FT_RawDays".Translate() + ": " + rawStr + " (" + tracker.RawNutrition.ToString("F0") + " " + "FT_Nutr".Translate() + ")");

        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 26f;

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, y, inRect.width);
        GUI.color = Color.white;
        y += 4f;

        // === Scrollable content ===
        var pawns = tracker.PawnConsumptions;
        var foods = tracker.FoodItems;
        var meals = foods.Where(f => f.isMeal).OrderByDescending(f => f.nutrition).ToList();
        var rawFoods = foods.Where(f => !f.isMeal).OrderByDescending(f => f.nutrition).ToList();
        var spoiling = tracker.SpoilingFood;

        float mealsHeight = meals.Count > 0 ? 26f + meals.Count * 24f + 6f : 0f;
        float rawHeight = rawFoods.Count > 0 ? 26f + rawFoods.Count * 24f + 6f : 0f;
        // Each category: header 26 + pawns * 24 + padding 4; plus daily total header 26
        int categoryCount = 0;
        foreach (PawnFoodCategory cat in System.Enum.GetValues(typeof(PawnFoodCategory)))
            if (pawns.Any(p => p.category == cat)) categoryCount++;
        float pawnHeight = 26f + categoryCount * 30f + pawns.Count * 24f;
        float totalListHeight = mealsHeight + rawHeight + pawnHeight + 30f;

        Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, totalListHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float rowY = 0f;

        // === Cooked meals section ===
        if (meals.Count > 0)
        {
            GUI.color = GreenText;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f), "FT_MealsList".Translate());
            GUI.color = Color.white;
            rowY += 26f;

            rowY = DrawFoodList(viewRect.width, rowY, meals, GreenText);
            rowY += 6f;
        }

        // === Raw food section ===
        if (rawFoods.Count > 0)
        {
            GUI.color = YellowText;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f), "FT_RawList".Translate());
            GUI.color = Color.white;
            rowY += 26f;

            rowY = DrawFoodList(viewRect.width, rowY, rawFoods, YellowText);
            rowY += 6f;
        }

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, rowY, viewRect.width);
        GUI.color = Color.white;
        rowY += 4f;

        // === Pawn consumption ===
        GUI.color = DimText;
        Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f),
            "FT_DailyTotal".Translate(tracker.DailyConsumption.ToString("F2")));
        GUI.color = Color.white;
        rowY += 26f;

        rowY = DrawPawnGroup(viewRect.width, rowY, pawns, PawnFoodCategory.Colonist, "FT_Colonists");
        rowY = DrawPawnGroup(viewRect.width, rowY, pawns, PawnFoodCategory.Prisoner, "FT_Prisoners");
        rowY = DrawPawnGroup(viewRect.width, rowY, pawns, PawnFoodCategory.Slave, "FT_Slaves");
        rowY = DrawPawnGroup(viewRect.width, rowY, pawns, PawnFoodCategory.Guest, "FT_Guests");

        Widgets.EndScrollView();
    }

    private static readonly Color ColonistColor = new Color(0.8f, 0.8f, 1f);
    private static readonly Color PrisonerColor = new Color(1f, 0.7f, 0.4f);
    private static readonly Color SlaveColor = new Color(1f, 0.5f, 0.5f);
    private static readonly Color GuestColor = new Color(0.6f, 0.9f, 0.6f);

    private static Color GetCategoryColor(PawnFoodCategory cat)
    {
        switch (cat)
        {
            case PawnFoodCategory.Prisoner: return PrisonerColor;
            case PawnFoodCategory.Slave: return SlaveColor;
            case PawnFoodCategory.Guest: return GuestColor;
            default: return ColonistColor;
        }
    }

    private float DrawPawnGroup(float width, float rowY, List<PawnFoodInfo> allPawns, PawnFoodCategory category, string labelKey)
    {
        var group = allPawns.Where(p => p.category == category).ToList();
        if (group.Count == 0)
            return rowY;

        float groupTotal = group.Sum(p => p.dailyNutrition);
        Color catColor = GetCategoryColor(category);

        GUI.color = catColor;
        Widgets.Label(new Rect(0f, rowY, width, 24f),
            labelKey.Translate() + " (" + group.Count + ") — " + groupTotal.ToString("F2") + " / " + "FT_PerDay".Translate());
        GUI.color = Color.white;
        rowY += 26f;

        for (int i = 0; i < group.Count; i++)
        {
            var info = group[i];
            Rect rowRect = new Rect(0f, rowY, width, 22f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            Widgets.Label(new Rect(15f, rowY, width * 0.6f, 22f), info.pawnName);

            GUI.color = DimText;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(width - 120f, rowY, 115f, 22f),
                info.dailyNutrition.ToString("F2") + " / " + "FT_PerDay".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 24f;
        }

        rowY += 4f;
        return rowY;
    }

    private float DrawFoodList(float width, float startY, List<FoodItemInfo> items, Color labelColor)
    {
        float rowY = startY;
        for (int i = 0; i < items.Count; i++)
        {
            var food = items[i];
            Rect rowRect = new Rect(0f, rowY, width, 22f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            // Count (right-aligned in 50px, fits x9999)
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(0f, rowY, 50f, 22f), "x" + food.count);
            Text.Anchor = TextAnchor.UpperLeft;

            // Icon
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(food.defName);
            if (def != null)
                Widgets.ThingIcon(new Rect(54f, rowY + 1f, 20f, 20f), def);

            // Name — aligned
            Widgets.Label(new Rect(78f, rowY, width * 0.38f, 22f), food.label);

            GUI.color = labelColor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(width - 120f, rowY, 115f, 22f),
                "(" + food.nutrition.ToString("F1") + " " + "FT_Nutr".Translate() + ")");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 24f;
        }
        return rowY;
    }

    private Color GetDaysColor(float days)
    {
        if (days > 10f) return GreenText;
        if (days > 4f) return YellowText;
        return RedText;
    }
}
