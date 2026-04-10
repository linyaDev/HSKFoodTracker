using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class Dialog_SpoilingFood : Window
{
    private Vector2 scrollPosition;

    private static readonly Color RedText = new Color(0.95f, 0.4f, 0.4f);
    private static readonly Color YellowText = new Color(0.95f, 0.95f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color RowBg = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    public override Vector2 InitialSize => new Vector2(400f, 450f);

    public Dialog_SpoilingFood()
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

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "FT_SpoilingTitle".Translate());
        Text.Font = GameFont.Small;

        float y = 40f;

        // Stats
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = RedText;
        Widgets.Label(new Rect(0f, y, inRect.width / 2f, 22f),
            "FT_SpoilIn2d".Translate(tracker.SpoilingIn2DaysNutrition.ToString("F1")));
        GUI.color = YellowText;
        Widgets.Label(new Rect(inRect.width / 2f, y, inRect.width / 2f, 22f),
            "FT_SpoilIn5d".Translate(tracker.SpoilingIn5DaysNutrition.ToString("F1")));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 28f;

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, y, inRect.width);
        GUI.color = Color.white;
        y += 4f;

        // Split into 2-day and 5-day lists
        var spoiling = tracker.SpoilingFood;
        var in2days = spoiling.Where(f => f.daysLeft <= 2f).ToList();
        var in5days = spoiling.Where(f => f.daysLeft > 2f).ToList();

        float section2h = in2days.Count > 0 ? 26f + in2days.Count * 24f + 6f : 0f;
        float section5h = in5days.Count > 0 ? 26f + in5days.Count * 24f : 0f;
        float listHeight = section2h + section5h;

        Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, listHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float rowY = 0f;

        // Within 2 days
        if (in2days.Count > 0)
        {
            GUI.color = RedText;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f), "FT_Within2Days".Translate());
            GUI.color = Color.white;
            rowY += 26f;
            rowY = DrawSpoilList(viewRect.width, rowY, in2days, RedText);
            rowY += 6f;
        }

        // Within 5 days
        if (in5days.Count > 0)
        {
            GUI.color = YellowText;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f), "FT_Within5Days".Translate());
            GUI.color = Color.white;
            rowY += 26f;
            DrawSpoilList(viewRect.width, rowY, in5days, YellowText);
        }

        Widgets.EndScrollView();
    }

    private float DrawSpoilList(float width, float startY, List<SpoilingFoodInfo> items, Color labelColor)
    {
        float rowY = startY;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Rect rowRect = new Rect(0f, rowY, width, 22f);

            if (i % 2 == 0)
                Widgets.DrawBoxSolid(rowRect, RowBg);

            // Quantity + icon + name (aligned)
            GUI.color = labelColor;
            Widgets.Label(new Rect(2f, rowY, 35f, 22f), "x" + item.count);

            // Try to find ThingDef for icon (white color for icon)
            GUI.color = Color.white;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(item.defName);
            if (def != null)
            {
                if (def.uiIcon != null && def.uiIcon != BaseContent.BadTex)
                    GUI.DrawTexture(new Rect(38f, rowY + 1f, 20f, 20f), def.uiIcon, ScaleMode.ScaleToFit);
            }

            GUI.color = labelColor;
            Widgets.Label(new Rect(62f, rowY, width * 0.35f, 22f), item.label);

            // Days left + nutrition (right aligned)
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(width - 160f, rowY, 155f, 22f),
                item.daysLeft.ToString("F1") + " " + "FT_Days".Translate()
                + "  (" + item.nutrition.ToString("F1") + " " + "FT_Nutr".Translate() + ")");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            rowY += 24f;
        }
        return rowY;
    }
}
