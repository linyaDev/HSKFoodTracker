using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class FoodTrackerOverlay : GameComponent
{
    private static readonly Color Green = new Color(0.3f, 0.9f, 0.3f);
    private static readonly Color Yellow = new Color(0.9f, 0.9f, 0.3f);
    private static readonly Color Red = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color BgColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    private static readonly Color PulseColor = new Color(1f, 0.1f, 0.1f);
    private static readonly Color PulseBg = new Color(0.4f, 0f, 0f);

    private static bool dragging;
    private static Vector2 dragOffset;
    private const float Width = 175f;

    // Cached display data — rebuilt only on Recalculate
    private static int cachedVersion = -1;
    private static string cachedLine1;
    private static string cachedSpoil2;
    private static string cachedSpoil5;
    private static string cachedAnimals;
    private static string cachedTooltip;
    private static Color cachedMainColor;
    private static Color cachedAnimalColor;
    private static bool cachedNoMeals;
    private static bool cachedHasSpoil2;
    private static bool cachedHasSpoil5;
    private static bool cachedShowAnimals;
    private static float cachedHeight;

    public FoodTrackerOverlay(Game game) : base()
    {
    }

    private static void RebuildCache(MapComponent_FoodTracker tracker)
    {
        int ver = tracker.Version;
        if (ver == cachedVersion)
            return;
        cachedVersion = ver;

        var settings = HSKFoodTrackerMod.Settings;
        float days = tracker.TotalDays;

        // Main color
        cachedNoMeals = tracker.MealDays < 0.1f;
        if (days > 10f)
            cachedMainColor = cachedNoMeals ? Yellow : Green;
        else if (days > 4f)
            cachedMainColor = Yellow;
        else
            cachedMainColor = Red;

        // Line 1
        string mealStr = tracker.MealDays >= 999f ? "∞" : tracker.MealDays.ToString("F1");
        string rawStr = tracker.RawDays >= 999f ? "∞" : tracker.RawDays.ToString("F1");
        cachedLine1 = string.Format("FT_WidgetFood2".Translate().RawText, mealStr, rawStr);

        // Spoil lines
        cachedHasSpoil2 = tracker.SpoilingIn2DaysNutrition >= 1f;
        if (cachedHasSpoil2)
        {
            float spoilDays = tracker.DailyConsumption > 0.01f
                ? tracker.SpoilingIn2DaysNutrition / tracker.DailyConsumption : 0f;
            cachedSpoil2 = string.Format("FT_WidgetSpoil2".Translate().RawText, spoilDays.ToString("F1"));
        }

        cachedHasSpoil5 = tracker.SpoilingIn5DaysNutrition >= 1f;
        if (cachedHasSpoil5)
        {
            float spoilDays5 = tracker.DailyConsumption > 0.01f
                ? tracker.SpoilingIn5DaysNutrition / tracker.DailyConsumption : 0f;
            cachedSpoil5 = string.Format("FT_WidgetSpoil5".Translate().RawText, spoilDays5.ToString("F1"));
        }

        // Animals
        cachedShowAnimals = settings?.showAnimalsInWidget == true && tracker.AnimalConsumption > 0.001f;
        if (cachedShowAnimals)
        {
            float feedDays = tracker.AnimalFeedDays;
            cachedAnimalColor = feedDays > 10f ? Green : (feedDays > 4f ? Yellow : Red);
            string feedStr = feedDays >= 999f ? "∞" : feedDays.ToString("F1");
            cachedAnimals = string.Format("FT_WidgetAnimals".Translate().RawText, feedStr);
        }

        // Tooltip
        cachedTooltip = "FT_WidgetTooltip".Translate(
            tracker.MealDays.ToString("F1"),
            tracker.RawDays.ToString("F1"),
            tracker.DailyConsumption.ToString("F1"))
            + "\n\n" + "FT_DragHint".Translate();

        // Height
        cachedHeight = 24f
            + (cachedHasSpoil2 ? 16f : 0f)
            + (cachedHasSpoil5 ? 16f : 0f)
            + (cachedShowAnimals ? 16f : 0f);
    }

    public override void GameComponentOnGUI()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        if (HSKFoodTrackerMod.Settings?.showWidget != true)
            return;

        var map = Find.CurrentMap;
        if (map == null)
            return;

        var tracker = map.GetComponent<MapComponent_FoodTracker>();
        if (tracker == null)
            return;

        RebuildCache(tracker);

        // Position
        var settings = HSKFoodTrackerMod.Settings;
        float posX = settings.widgetX;
        float posY = settings.widgetY;
        if (posX < 0f)
        {
            posX = 200f;
            posY = 200f;
            settings.widgetX = posX;
            settings.widgetY = posY;
        }

        Rect widgetRect = new Rect(posX, posY, Width, cachedHeight);

        // Dragging
        var evt = Event.current;
        if (evt.type == EventType.MouseDown && Mouse.IsOver(widgetRect) && evt.button == 1)
        {
            dragging = true;
            dragOffset = evt.mousePosition - new Vector2(posX, posY);
            evt.Use();
        }
        if (dragging)
        {
            if (evt.type == EventType.MouseDrag || evt.type == EventType.MouseMove)
            {
                var newPos = evt.mousePosition - dragOffset;
                posX = Mathf.Clamp(newPos.x, 0f, UI.screenWidth - Width);
                posY = Mathf.Clamp(newPos.y, 0f, UI.screenHeight - cachedHeight);
                settings.widgetX = posX;
                settings.widgetY = posY;
                widgetRect = new Rect(posX, posY, Width, cachedHeight);
            }
            if (evt.type == EventType.MouseUp && evt.button == 1)
            {
                dragging = false;
                settings.Write();
                evt.Use();
            }
        }

        // Background
        Widgets.DrawBoxSolid(widgetRect, BgColor);
        Widgets.DrawBox(widgetRect, 1);

        // Main color + pulse
        GUI.color = cachedMainColor;
        if (cachedNoMeals)
        {
            float pulse = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
            GUI.color = Color.Lerp(cachedMainColor, PulseColor, pulse * 0.5f);
            var bg = PulseBg;
            bg.a = 0.15f * pulse;
            Widgets.DrawBoxSolid(widgetRect, bg);
        }

        // Line 1: Food days
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(widgetRect.x, widgetRect.y + 2f, Width, 20f), cachedLine1);

        float lineY = widgetRect.y + 22f;
        Text.Font = GameFont.Tiny;

        // Line 2: Spoiling in 2 days
        if (cachedHasSpoil2)
        {
            GUI.color = Red;
            Rect spoilRect = new Rect(widgetRect.x, lineY, Width, 16f);
            Widgets.Label(spoilRect, cachedSpoil2);
            if (Widgets.ButtonInvisible(spoilRect))
                Find.WindowStack.Add(new Dialog_SpoilingFood());
            lineY += 16f;
        }

        // Line 3: Spoiling in 5 days
        if (cachedHasSpoil5)
        {
            GUI.color = Yellow;
            Rect spoilRect5 = new Rect(widgetRect.x, lineY, Width, 16f);
            Widgets.Label(spoilRect5, cachedSpoil5);
            if (Widgets.ButtonInvisible(spoilRect5))
                Find.WindowStack.Add(new Dialog_SpoilingFood());
            lineY += 16f;
        }

        // Bottom line: animal feed days
        if (cachedShowAnimals)
        {
            GUI.color = cachedAnimalColor;
            Widgets.Label(new Rect(widgetRect.x, lineY, Width, 16f), cachedAnimals);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Left-click to open details
        if (Widgets.ButtonInvisible(widgetRect) && !dragging)
            Find.WindowStack.Add(new Dialog_FoodDetails());

        // Tooltip
        if (Mouse.IsOver(widgetRect))
        {
            Widgets.DrawHighlight(widgetRect);
            TooltipHandler.TipRegion(widgetRect, cachedTooltip);
        }
    }
}
