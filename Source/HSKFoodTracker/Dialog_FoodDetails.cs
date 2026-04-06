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

    public override Vector2 InitialSize => new Vector2(450f, 600f);

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
        var meals = foods.Where(f => f.isMeal).ToList();
        var rawFoods = foods.Where(f => !f.isMeal).ToList();
        var spoiling = tracker.SpoilingFood;

        float mealsHeight = meals.Count > 0 ? 26f + meals.Count * 24f + 6f : 0f;
        float rawHeight = rawFoods.Count > 0 ? 26f + rawFoods.Count * 24f + 6f : 0f;
        float pawnHeight = 26f + pawns.Count * 24f;
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

        for (int i = 0; i < pawns.Count; i++)
        {
            var info = pawns[i];
            Rect rowRect = new Rect(0f, rowY, viewRect.width, 22f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            Widgets.Label(new Rect(5f, rowY, viewRect.width * 0.6f, 22f), info.pawnName);

            GUI.color = DimText;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(viewRect.width - 120f, rowY, 115f, 22f),
                info.dailyNutrition.ToString("F2") + " / " + "FT_PerDay".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 24f;
        }

        Widgets.EndScrollView();
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

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(food.defName);
            if (def != null)
                Widgets.ThingIcon(new Rect(2f, rowY, 20f, 20f), def);

            Widgets.Label(new Rect(25f, rowY, width * 0.5f, 22f), food.label);

            GUI.color = labelColor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(width - 180f, rowY, 175f, 22f),
                "x" + food.count + "  (" + food.nutrition.ToString("F1") + " " + "FT_Nutr".Translate() + ")");
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
