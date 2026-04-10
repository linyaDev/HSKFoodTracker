using System.Collections.Generic;
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

    // Fire particles
    private struct Ember
    {
        public float x, y, speed, life, maxLife, size;
    }
    private static readonly List<Ember> embers = new List<Ember>();
    private static float lastEmberTime;
    private static readonly Color EmberOrange = new Color(1f, 0.6f, 0.1f);
    private static readonly Color EmberRed = new Color(1f, 0.2f, 0.05f);

    private static bool dragging;
    private static Vector2 dragOffset;
    private const float Width = 175f;

    private static Vector2 WidgetPos
    {
        get
        {
            var s = HSKFoodTrackerMod.Settings;
            if (s == null) return new Vector2(200f, 200f);
            return new Vector2(s.widgetX, s.widgetY);
        }
        set
        {
            var s = HSKFoodTrackerMod.Settings;
            if (s == null) return;
            s.widgetX = value.x;
            s.widgetY = value.y;
        }
    }

    public FoodTrackerOverlay(Game game) : base()
    {
    }

    public override void GameComponentOnGUI()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        if (Find.CurrentMap == null)
            return;

        var tracker = Find.CurrentMap.GetComponent<MapComponent_FoodTracker>();
        if (tracker == null)
            return;

        // Default position
        var pos = WidgetPos;
        if (pos.x < 0f)
        {
            pos = new Vector2(200f, 200f);
            WidgetPos = pos;
        }

        float days = tracker.TotalDays;
        bool hasSpoil2 = tracker.SpoilingIn2DaysNutrition >= 1f;
        bool hasSpoil5 = tracker.SpoilingIn5DaysNutrition >= 1f;
        float height = 24f + (hasSpoil2 ? 16f : 0f) + (hasSpoil5 ? 16f : 0f);
        Rect widgetRect = new Rect(pos.x, pos.y, Width, height);

        // Dragging
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(widgetRect) && Event.current.button == 1)
        {
            dragging = true;
            dragOffset = Event.current.mousePosition - pos;
            Event.current.Use();
        }
        if (dragging)
        {
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
            {
                pos = Event.current.mousePosition - dragOffset;
                pos.x = Mathf.Clamp(pos.x, 0f, UI.screenWidth - Width);
                pos.y = Mathf.Clamp(pos.y, 0f, UI.screenHeight - height);
                WidgetPos = pos;
                widgetRect = new Rect(pos.x, pos.y, Width, height);
            }
            if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
            {
                dragging = false;
                HSKFoodTrackerMod.Settings?.Write();
                Event.current.Use();
            }
        }

        // Background
        Widgets.DrawBoxSolid(widgetRect, BgColor);
        Widgets.DrawBox(widgetRect, 1);

        // Color based on days
        bool noMeals = tracker.MealDays < 0.1f;
        if (days > 10f)
            GUI.color = noMeals ? Yellow : Green;
        else if (days > 4f)
            GUI.color = Yellow;
        else if (days > 0.1f)
            GUI.color = Red;
        else
            GUI.color = Red;

        // Pulsing + embers when no cooked meals
        if (noMeals)
        {
            float pulse = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
            GUI.color = Color.Lerp(GUI.color, new Color(1f, 0.1f, 0.1f), pulse * 0.5f);
            Widgets.DrawBoxSolid(widgetRect, new Color(0.4f, 0f, 0f, 0.15f * pulse));
            SpawnEmbers(widgetRect);
        }

        // Update and draw embers
        DrawEmbers();

        // Line 1: Food days
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;

        string mealStr = tracker.MealDays >= 999f ? "∞" : tracker.MealDays.ToString("F1");
        string rawStr = tracker.RawDays >= 999f ? "∞" : tracker.RawDays.ToString("F1");
        Rect line1 = new Rect(widgetRect.x, widgetRect.y + 2f, widgetRect.width, 20f);
        Widgets.Label(line1, string.Format("FT_WidgetFood2".Translate().RawText, mealStr, rawStr));

        float lineY = widgetRect.y + 22f;
        Text.Font = GameFont.Tiny;

        // Line 2: Spoiling in 2 days
        if (hasSpoil2)
        {
            GUI.color = Red;
            Rect spoilRect = new Rect(widgetRect.x, lineY, widgetRect.width, 16f);
            float spoilDays = tracker.DailyConsumption > 0.01f
                ? tracker.SpoilingIn2DaysNutrition / tracker.DailyConsumption : 0f;
            Widgets.Label(spoilRect, string.Format("FT_WidgetSpoil2".Translate().RawText, spoilDays.ToString("F1")));
            if (Widgets.ButtonInvisible(spoilRect))
                Find.WindowStack.Add(new Dialog_SpoilingFood());
            lineY += 16f;
        }

        // Line 3: Spoiling in 5 days
        if (hasSpoil5)
        {
            GUI.color = Yellow;
            Rect spoilRect5 = new Rect(widgetRect.x, lineY, widgetRect.width, 16f);
            float spoilDays5 = tracker.DailyConsumption > 0.01f
                ? tracker.SpoilingIn5DaysNutrition / tracker.DailyConsumption : 0f;
            Widgets.Label(spoilRect5, string.Format("FT_WidgetSpoil5".Translate().RawText, spoilDays5.ToString("F1")));
            if (Widgets.ButtonInvisible(spoilRect5))
                Find.WindowStack.Add(new Dialog_SpoilingFood());
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        // Left-click to open details
        if (Widgets.ButtonInvisible(widgetRect) && !dragging)
        {
            Find.WindowStack.Add(new Dialog_FoodDetails());
        }

        // Tooltip
        if (Mouse.IsOver(widgetRect))
        {
            Widgets.DrawHighlight(widgetRect);
            TooltipHandler.TipRegion(widgetRect, "FT_WidgetTooltip".Translate(
                tracker.MealDays.ToString("F1"),
                tracker.RawDays.ToString("F1"),
                tracker.DailyConsumption.ToString("F1"))
                + "\n\n" + "FT_DragHint".Translate());
        }
    }

    private static void SpawnEmbers(Rect widgetRect)
    {
        float now = Time.realtimeSinceStartup;
        if (now - lastEmberTime < 0.15f)
            return;
        lastEmberTime = now;

        embers.Add(new Ember
        {
            x = widgetRect.x + Random.Range(5f, widgetRect.width - 5f),
            y = widgetRect.yMax,
            speed = Random.Range(15f, 35f),
            life = 0f,
            maxLife = Random.Range(0.8f, 1.5f),
            size = Random.Range(3f, 6f)
        });
    }

    private static void DrawEmbers()
    {
        if (embers.Count == 0)
            return;

        float dt = Time.deltaTime;
        for (int i = embers.Count - 1; i >= 0; i--)
        {
            var e = embers[i];
            e.life += dt;
            if (e.life >= e.maxLife)
            {
                embers.RemoveAt(i);
                continue;
            }
            e.y += e.speed * dt;
            e.x += Mathf.Sin(e.life * 5f) * 0.5f;
            embers[i] = e;

            float t = e.life / e.maxLife;
            Color c = Color.Lerp(EmberOrange, EmberRed, t);
            c.a = 1f - t;
            float s = e.size * (1f - t * 0.5f);
            GUI.color = c;
            Widgets.DrawBoxSolid(new Rect(e.x - s / 2f, e.y - s / 2f, s, s), c);
        }
        GUI.color = Color.white;
    }
}
