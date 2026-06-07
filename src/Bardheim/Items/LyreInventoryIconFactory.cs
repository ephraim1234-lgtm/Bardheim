using UnityEngine;

namespace Bardheim.Items;

internal static class LyreInventoryIconFactory
{
    private const int Size = 128;
    private const int IconStringCount = 6;

    public static Sprite Create()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "Bardheim_GeneratedInventoryIcon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Clear(texture, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        DrawLyre(texture);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, Size, Size),
            new Vector2(0.5f, 0.5f),
            Size);
    }

    private static void DrawLyre(Texture2D texture)
    {
        var shadow = new Color(0.06f, 0.035f, 0.02f, 0.55f);
        var darkWood = new Color(0.30f, 0.14f, 0.055f, 1.0f);
        var midWood = new Color(0.58f, 0.29f, 0.11f, 1.0f);
        var lightWood = new Color(0.85f, 0.48f, 0.20f, 1.0f);
        var stringColor = new Color(0.96f, 0.86f, 0.62f, 1.0f);

        DrawLine(texture, 37, 103, 29, 32, shadow, 8);
        DrawLine(texture, 91, 103, 99, 32, shadow, 8);
        DrawLine(texture, 32, 32, 96, 32, shadow, 8);
        DrawLine(texture, 38, 98, 90, 98, shadow, 13);

        DrawLine(texture, 36, 101, 28, 31, darkWood, 7);
        DrawLine(texture, 92, 101, 100, 31, darkWood, 7);
        DrawLine(texture, 30, 31, 98, 31, darkWood, 7);
        DrawLine(texture, 38, 97, 90, 97, darkWood, 13);

        DrawLine(texture, 39, 98, 31, 35, midWood, 4);
        DrawLine(texture, 89, 98, 97, 35, midWood, 4);
        DrawLine(texture, 35, 35, 93, 35, midWood, 4);
        DrawLine(texture, 42, 93, 86, 93, midWood, 8);

        DrawLine(texture, 43, 91, 85, 91, lightWood, 2);
        DrawLine(texture, 39, 42, 89, 42, lightWood, 2);
        DrawDecorativeWoodwork(texture, darkWood, lightWood);

        for (var index = 0; index < IconStringCount; index++)
        {
            var x = 44 + index * 8;
            DrawLine(texture, x, 88, x, 42, stringColor, 2);
        }

        for (var index = 0; index < 6; index++)
        {
            var x = 46 + index * 7;
            DrawRect(texture, x, 25, 4, 8, lightWood);
            DrawRect(texture, x + 1, 24, 2, 2, stringColor);
        }
    }

    private static void DrawDecorativeWoodwork(Texture2D texture, Color darkWood, Color lightWood)
    {
        DrawVikingKnotwork(texture, lightWood);
        DrawLine(texture, 40, 101, 88, 101, darkWood, 3);
        DrawLine(texture, 42, 84, 86, 84, lightWood, 2);
        DrawLine(texture, 52, 96, 50, 103, lightWood, 2);
        DrawLine(texture, 76, 96, 78, 103, lightWood, 2);
    }

    private static void DrawVikingKnotwork(Texture2D texture, Color lightWood)
    {
        DrawKnot(texture, 36, 58, lightWood);
        DrawKnot(texture, 92, 58, lightWood);
        DrawKnot(texture, 37, 72, lightWood);
        DrawKnot(texture, 91, 72, lightWood);
        DrawLine(texture, 55, 101, 60, 96, lightWood, 2);
        DrawLine(texture, 55, 96, 60, 101, lightWood, 2);
        DrawLine(texture, 68, 101, 73, 96, lightWood, 2);
        DrawLine(texture, 68, 96, 73, 101, lightWood, 2);
    }

    private static void DrawKnot(Texture2D texture, int centerX, int centerY, Color color)
    {
        DrawLine(texture, centerX - 4, centerY, centerX, centerY - 4, color, 2);
        DrawLine(texture, centerX, centerY - 4, centerX + 4, centerY, color, 2);
        DrawLine(texture, centerX + 4, centerY, centerX, centerY + 4, color, 2);
        DrawLine(texture, centerX, centerY + 4, centerX - 4, centerY, color, 2);
    }

    private static void Clear(Texture2D texture, Color color)
    {
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (var yy = y; yy < y + height; yy++)
        {
            for (var xx = x; xx < x + width; xx++)
            {
                SetPixel(texture, xx, yy, color);
            }
        }
    }

    private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        var dx = Mathf.Abs(x1 - x0);
        var dy = -Mathf.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            DrawBrush(texture, x0, y0, thickness, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var nextError = 2 * error;
            if (nextError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (nextError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawBrush(Texture2D texture, int centerX, int centerY, int thickness, Color color)
    {
        var radius = thickness / 2;
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                SetPixel(texture, x, y, color);
            }
        }
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}
