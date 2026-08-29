using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class FoodTrackerOverlay : GameComponent
{
    // EPrime's Readouts palette: muted green / LowTint / CriticalTint
    private static readonly Color Green = new Color(0.55f, 0.78f, 0.45f);
    private static readonly Color Yellow = new Color(1f, 0.92f, 0.55f);
    private static readonly Color Red = new Color(0.9f, 0.46f, 0.42f);
    private static readonly Color BgColor = new Color(0.08f, 0.08f, 0.08f, 0.7f);
    private static readonly Color PulseColor = new Color(1f, 0.1f, 0.1f);
    private static readonly Color PulseBg = new Color(0.4f, 0f, 0f);

    private static bool dragging;
    private static Vector2 dragOffset;
    private const float Width = 175f;

    // Cached references
    private static Map cachedMap;
    private static MapComponent_FoodTracker cachedTracker;

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

    private static MapComponent_FoodTracker GetTracker()
    {
        var map = Find.CurrentMap;
        if (map == null) return null;
        if (map == cachedMap && cachedTracker != null) return cachedTracker;
        cachedMap = map;
        cachedTracker = map.GetComponent<MapComponent_FoodTracker>();
        return cachedTracker;
    }

    private static void RebuildCache(MapComponent_FoodTracker tracker)
    {
        int ver = tracker.Version;
        if (ver == cachedVersion)
            return;
        cachedVersion = ver;

        var settings = HSKFoodTrackerMod.Settings;
        float days = tracker.TotalDays;

        cachedNoMeals = tracker.MealDays < 0.1f;
        if (days > 10f)
            cachedMainColor = cachedNoMeals ? Yellow : Green;
        else if (days > 4f)
            cachedMainColor = Yellow;
        else
            cachedMainColor = Red;

        string mealStr = tracker.MealDays >= 999f ? "∞" : tracker.MealDays.ToString("F1");
        string rawStr = tracker.RawDays >= 999f ? "∞" : tracker.RawDays.ToString("F1");
        cachedLine1 = string.Format("FT_WidgetFood2".Translate().RawText, mealStr, rawStr);

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

        cachedShowAnimals = settings?.showAnimalsInWidget == true && tracker.AnimalConsumption > 0.001f;
        if (cachedShowAnimals)
        {
            float feedDays = tracker.AnimalFeedDays;
            cachedAnimalColor = feedDays > 10f ? Green : (feedDays > 4f ? Yellow : Red);
            string feedStr = feedDays >= 999f ? "∞" : feedDays.ToString("F1");
            cachedAnimals = string.Format("FT_WidgetAnimals".Translate().RawText, feedStr);
        }

        cachedTooltip = "FT_WidgetTooltip".Translate(
            tracker.MealDays.ToString("F1"),
            tracker.RawDays.ToString("F1"),
            tracker.DailyConsumption.ToString("F1"))
            + "\n\n" + "FT_DragHint".Translate();

        cachedHeight = 24f
            + (cachedHasSpoil2 ? 16f : 0f)
            + (cachedHasSpoil5 ? 16f : 0f)
            + (cachedShowAnimals ? 16f : 0f);
    }

    // Drawn from Patch_ReadoutDraw (after map thing labels), not from GameComponentOnGUI —
    // that hook runs before ThingOverlays, so stack-count labels bled through the widget.
    public void DrawOverlay()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        var settings = HSKFoodTrackerMod.Settings;
        if (settings?.showWidget != true)
            return;

        var tracker = GetTracker();
        if (tracker == null)
            return;

        RebuildCache(tracker);

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

        var evt = Event.current;
        var evtType = evt.type;

        // Handle input on mouse events
        if (evtType == EventType.MouseDown && evt.button == 1 && Mouse.IsOver(widgetRect))
        {
            dragging = true;
            dragOffset = evt.mousePosition - new Vector2(posX, posY);
            evt.Use();
            return;
        }

        if (dragging)
        {
            if (evtType == EventType.MouseDrag || evtType == EventType.MouseMove)
            {
                var newPos = evt.mousePosition - dragOffset;
                settings.widgetX = Mathf.Clamp(newPos.x, 0f, UI.screenWidth - Width);
                settings.widgetY = Mathf.Clamp(newPos.y, 0f, UI.screenHeight - cachedHeight);
                return;
            }
            if (evtType == EventType.MouseUp && evt.button == 1)
            {
                dragging = false;
                settings.Write();
                evt.Use();
                return;
            }
        }

        // Only draw on Repaint
        if (evtType != EventType.Repaint)
        {
            if (evtType == EventType.MouseDown && evt.button == 0 && Mouse.IsOver(widgetRect))
            {
                // Spoil clicks — check first, they overlap the widget rect
                float lineY = posY + 22f;
                if (cachedHasSpoil2)
                {
                    if (Mouse.IsOver(new Rect(posX, lineY, Width, 16f)))
                    {
                        Find.WindowStack.Add(new Dialog_SpoilingFood());
                        evt.Use();
                        return;
                    }
                    lineY += 16f;
                }
                if (cachedHasSpoil5 && Mouse.IsOver(new Rect(posX, lineY, Width, 16f)))
                {
                    Find.WindowStack.Add(new Dialog_SpoilingFood());
                    evt.Use();
                    return;
                }

                // General click — open food details
                Find.WindowStack.Add(new Dialog_FoodDetails());
                evt.Use();
            }
            return;
        }

        // === Repaint only below ===
        Widgets.DrawBoxSolid(widgetRect, BgColor);

        GUI.color = cachedMainColor;
        if (cachedNoMeals)
        {
            float pulse = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
            GUI.color = Color.Lerp(cachedMainColor, PulseColor, pulse * 0.5f);
            var bg = PulseBg;
            bg.a = 0.15f * pulse;
            Widgets.DrawBoxSolid(widgetRect, bg);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(posX, posY + 2f, Width, 20f), cachedLine1);

        float drawY = posY + 22f;
        Text.Font = GameFont.Tiny;

        if (cachedHasSpoil2)
        {
            GUI.color = Red;
            Widgets.Label(new Rect(posX, drawY, Width, 16f), cachedSpoil2);
            drawY += 16f;
        }

        if (cachedHasSpoil5)
        {
            GUI.color = Yellow;
            Widgets.Label(new Rect(posX, drawY, Width, 16f), cachedSpoil5);
            drawY += 16f;
        }

        if (cachedShowAnimals)
        {
            GUI.color = cachedAnimalColor;
            Widgets.Label(new Rect(posX, drawY, Width, 16f), cachedAnimals);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        if (Mouse.IsOver(widgetRect))
        {
            Widgets.DrawHighlight(widgetRect);
            TooltipHandler.TipRegion(widgetRect, cachedTooltip);
        }
    }
}
