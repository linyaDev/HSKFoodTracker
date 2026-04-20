using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

public class Dialog_AnimalFood : Window
{
    private Vector2 scrollPosition;
    private readonly Window parent;

    private static readonly Color GreenText = new Color(0.4f, 0.95f, 0.4f);
    private static readonly Color YellowText = new Color(0.95f, 0.95f, 0.4f);
    private static readonly Color RedText = new Color(0.95f, 0.4f, 0.4f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color RowBg = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    private static readonly Color AnimalColor = new Color(0.7f, 0.85f, 1f);

    public override Vector2 InitialSize => new Vector2(420f, 500f);

    public Dialog_AnimalFood(Window parent)
    {
        this.parent = parent;
        doCloseButton = true;
        doCloseX = true;
        draggable = true;
        absorbInputAroundWindow = false;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (parent != null)
        {
            windowRect.x = parent.windowRect.xMax + 4f;
            windowRect.y = parent.windowRect.y;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        var map = Find.CurrentMap;
        if (map == null) return;

        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "FT_AnimalTitle".Translate());
        Text.Font = GameFont.Small;

        float y = 38f;

        // Collect animal data
        var animals = map.mapPawns.SpawnedColonyAnimals
            .Where(a => a.needs?.food != null && !a.Dead)
            .OrderByDescending(a => a.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed))
            .ToList();

        float totalAnimalConsumption = 0f;
        var animalInfos = new List<AnimalFoodInfo>();
        foreach (var animal in animals)
        {
            float perTick = animal.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed);
            float perDay = perTick * 60000f;
            totalAnimalConsumption += perDay;
            animalInfos.Add(new AnimalFoodInfo
            {
                pawnName = animal.LabelShortCap,
                defName = animal.def.defName,
                dailyNutrition = perDay
            });
        }

        // Collect animal feed stocks
        var feedStocks = new List<FeedStockInfo>();
        foreach (var kvp in map.resourceCounter.AllCountedAmounts)
        {
            var def = kvp.Key;
            if (kvp.Value <= 0) continue;
            if (!IsAnimalFeed(def)) continue;

            float nutrition = def.GetStatValueAbstract(StatDefOf.Nutrition) * kvp.Value;
            feedStocks.Add(new FeedStockInfo
            {
                defName = def.defName,
                label = def.LabelCap,
                count = kvp.Value,
                nutrition = nutrition
            });
        }
        feedStocks.Sort((a, b) => b.nutrition.CompareTo(a.nutrition));

        float totalFeedNutrition = feedStocks.Sum(f => f.nutrition);
        float feedDays = totalAnimalConsumption > 0.001f ? totalFeedNutrition / totalAnimalConsumption : 999f;

        // === Total days — big ===
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = GetDaysColor(feedDays);
        string totalStr = feedDays >= 999f ? "\u221e" : feedDays.ToString("F1");
        Widgets.Label(new Rect(0f, y, inRect.width, 30f),
            "FT_AnimalDays".Translate() + ": " + totalStr + " " + "FT_Days".Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 34f;

        // Consumption summary
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = DimText;
        Widgets.Label(new Rect(0f, y, inRect.width, 22f),
            "FT_AnimalConsumption".Translate(totalAnimalConsumption.ToString("F2")));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        y += 26f;

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, y, inRect.width);
        GUI.color = Color.white;
        y += 4f;

        // === Scrollable content ===
        float feedHeight = feedStocks.Count > 0 ? 26f + feedStocks.Count * 24f + 6f : 0f;
        float animalHeight = animalInfos.Count > 0 ? 26f + animalInfos.Count * 24f + 6f : 0f;
        float totalListHeight = feedHeight + animalHeight + 10f;

        Rect outRect = new Rect(0f, y, inRect.width, inRect.height - y - 50f);
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, totalListHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float rowY = 0f;

        // === Feed stocks section ===
        if (feedStocks.Count > 0)
        {
            GUI.color = GreenText;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f), "FT_FeedStocks".Translate());
            GUI.color = Color.white;
            rowY += 26f;

            for (int i = 0; i < feedStocks.Count; i++)
            {
                var feed = feedStocks[i];
                Rect rowRect = new Rect(0f, rowY, viewRect.width, 22f);
                if (i % 2 == 0)
                    Widgets.DrawBoxSolid(rowRect, RowBg);

                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(0f, rowY, 50f, 22f), "x" + feed.count);
                Text.Anchor = TextAnchor.UpperLeft;

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(feed.defName);
                if (def != null)
                    Widgets.ThingIcon(new Rect(54f, rowY + 1f, 20f, 20f), def);

                Widgets.Label(new Rect(78f, rowY, viewRect.width * 0.4f, 22f), feed.label);

                GUI.color = GreenText;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(viewRect.width - 130f, rowY, 120f, 22f),
                    "(" + feed.nutrition.ToString("F1") + " " + "FT_Nutr".Translate() + ")");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                rowY += 24f;
            }
            rowY += 6f;
        }

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        Widgets.DrawLineHorizontal(0f, rowY, viewRect.width);
        GUI.color = Color.white;
        rowY += 4f;

        // === Animals section ===
        if (animalInfos.Count > 0)
        {
            GUI.color = AnimalColor;
            Widgets.Label(new Rect(0f, rowY, viewRect.width, 24f),
                "FT_AnimalList".Translate(animalInfos.Count));
            GUI.color = Color.white;
            rowY += 26f;

            for (int i = 0; i < animalInfos.Count; i++)
            {
                var info = animalInfos[i];
                Rect rowRect = new Rect(0f, rowY, viewRect.width, 22f);
                if (i % 2 == 0)
                    Widgets.DrawBoxSolid(rowRect, RowBg);

                Widgets.Label(new Rect(15f, rowY, viewRect.width * 0.6f, 22f), info.pawnName);

                GUI.color = DimText;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(viewRect.width - 130f, rowY, 120f, 22f),
                    info.dailyNutrition.ToString("F2") + " / " + "FT_PerDay".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                rowY += 24f;
            }
        }

        Widgets.EndScrollView();
    }

    private bool IsAnimalFeed(ThingDef def)
    {
        if (!def.IsNutritionGivingIngestible) return false;
        return def.defName == "Hay" || def.defName == "Kibble" || def.defName == "Silage";
    }

    private Color GetDaysColor(float days)
    {
        if (days > 10f) return GreenText;
        if (days > 4f) return YellowText;
        return RedText;
    }

    private struct AnimalFoodInfo
    {
        public string pawnName;
        public string defName;
        public float dailyNutrition;
    }

    private struct FeedStockInfo
    {
        public string defName;
        public string label;
        public int count;
        public float nutrition;
    }
}
