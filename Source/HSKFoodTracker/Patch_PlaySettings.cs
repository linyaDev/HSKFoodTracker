using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HSKFoodTracker;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class Patch_PlaySettings
{
    private static Texture2D iconTexture;

    private static Texture2D Icon
    {
        get
        {
            if (iconTexture == null)
                iconTexture = MakeFoodIcon();
            return iconTexture;
        }
    }

    public static void Postfix(WidgetRow row, bool worldView)
    {
        if (worldView)
            return;

        var settings = HSKFoodTrackerMod.Settings;
        if (settings == null)
            return;

        row.ToggleableIcon(ref settings.showWidget, Icon,
            "FT_ToggleWidget".Translate(), SoundDefOf.Mouseover_ButtonToggle);
    }

    private static Texture2D MakeFoodIcon()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float cx = size / 2f;

        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;
        var dim = new Color(1f, 1f, 1f, 0.5f);

        // Clear
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, clear);

        // Bowl body — lower half ellipse
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float nx = (x - cx) / 26f;
                float ny = (y - 22f) / 18f;
                float d = nx * nx + ny * ny;
                if (d <= 1f && y <= 26)
                    tex.SetPixel(x, y, white);
            }
        }

        // Bowl rim — wide thin ellipse
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float nx = (x - cx) / 28f;
                float ny = (y - 26f) / 5f;
                float d = nx * nx + ny * ny;
                if (d <= 1f)
                    tex.SetPixel(x, y, white);
            }
        }

        // Food mound — bump above rim
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float nx = (x - cx) / 19f;
                float ny = (y - 32f) / 8f;
                float d = nx * nx + ny * ny;
                if (d <= 1f && y >= 26)
                    tex.SetPixel(x, y, white);
            }
        }

        // Steam — 3 wavy lines
        DrawSteam(tex, size, cx - 9f, 40f, dim);
        DrawSteam(tex, size, cx, 42f, dim);
        DrawSteam(tex, size, cx + 9f, 40f, dim);

        tex.Apply();
        return tex;
    }

    private static void DrawSteam(Texture2D tex, int size, float sx, float sy, Color col)
    {
        for (int i = 0; i < 14; i++)
        {
            float t = i / 14f;
            float x = sx + Mathf.Sin(t * Mathf.PI * 3f) * 3f;
            float y = sy + i;

            var c = col;
            c.a = col.a * (1f - t * 0.8f);
            int px = Mathf.RoundToInt(x);
            int py = Mathf.RoundToInt(y);
            if (px >= 0 && px < size && py >= 0 && py < size)
            {
                tex.SetPixel(px, py, c);
                if (px + 1 < size) tex.SetPixel(px + 1, py, c);
            }
        }
    }
}
