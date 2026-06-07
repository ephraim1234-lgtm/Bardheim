using UnityEngine;

namespace Bardheim.Items;

internal static class FluteInventoryIconFactory
{
    private const int Size = 128;

    public static Sprite Create()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "Bardheim_GeneratedFluteInventoryIcon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Clear(texture, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        DrawFlute(texture);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, Size, Size),
            new Vector2(0.5f, 0.5f),
            Size);
    }

    private static void DrawFlute(Texture2D texture)
    {
        var shadow = new Color(0.025f, 0.020f, 0.014f, 0.60f);
        var outline = new Color(0.13f, 0.060f, 0.025f, 1.0f);
        var midWood = new Color(0.66f, 0.40f, 0.16f, 1.0f);
        var lightWood = new Color(0.90f, 0.63f, 0.28f, 1.0f);
        var hole = new Color(0.025f, 0.018f, 0.012f, 1.0f);
        var bronze = new Color(0.58f, 0.43f, 0.18f, 1.0f);
        var bronzeLight = new Color(0.82f, 0.64f, 0.30f, 1.0f);
        var bone = new Color(0.78f, 0.65f, 0.42f, 1.0f);

        DrawCleanSilhouette(texture, shadow, outline);
        DrawTaperedBody(texture, midWood, lightWood);
        DrawSimpleEndCaps(texture, bronze, bronzeLight);
        DrawCarvedBands(texture, bronze, outline);
        DrawSubtleInlays(texture, bronzeLight);
        DrawRaisedMouthpiece(texture, bone, hole);
        DrawReadableToneHoles(texture, hole, bronzeLight);
        DrawFingerHoles(texture, hole, bronzeLight);
        DrawMouthNotch(texture, hole);
        DrawRuneMarks(texture, outline);
    }

    private static void DrawCleanSilhouette(Texture2D texture, Color shadow, Color outline)
    {
        DrawLine(texture, 18, 84, 106, 34, shadow, 17);
        DrawLine(texture, 16, 80, 104, 32, outline, 15);
    }

    private static void DrawTaperedBody(Texture2D texture, Color midWood, Color lightWood)
    {
        DrawLine(texture, 20, 78, 101, 33, midWood, 11);
        DrawLine(texture, 30, 73, 90, 39, lightWood, 4);
        DrawLine(texture, 39, 76, 93, 46, midWood, 2);
    }

    private static void DrawSimpleEndCaps(Texture2D texture, Color bronze, Color bronzeLight)
    {
        DrawLine(texture, 18, 80, 28, 74, bronze, 6);
        DrawLine(texture, 23, 81, 31, 76, bronzeLight, 2);
        DrawLine(texture, 97, 36, 107, 30, bronze, 6);
        DrawLine(texture, 101, 38, 109, 33, bronzeLight, 2);
    }

    private static void DrawFingerHoles(Texture2D texture, Color hole, Color rim)
    {
        var holes = new[]
        {
            new Vector2Int(43, 66),
            new Vector2Int(53, 60),
            new Vector2Int(63, 55),
            new Vector2Int(73, 49),
            new Vector2Int(83, 43),
            new Vector2Int(91, 39)
        };

        foreach (var point in holes)
        {
            DrawCircle(texture, point.x, point.y, 3, rim);
            DrawCircle(texture, point.x, point.y, 2, hole);
        }
    }

    private static void DrawReadableToneHoles(Texture2D texture, Color hole, Color rim)
    {
        var holes = new[]
        {
            new Vector2Int(43, 66),
            new Vector2Int(53, 60),
            new Vector2Int(63, 55),
            new Vector2Int(73, 49),
            new Vector2Int(83, 43),
            new Vector2Int(91, 39)
        };

        foreach (var point in holes)
        {
            DrawCircle(texture, point.x, point.y, 5, rim);
            DrawCircle(texture, point.x, point.y, 3, hole);
        }
    }

    private static void DrawCarvedBands(Texture2D texture, Color bronze, Color leather)
    {
        DrawLine(texture, 24, 78, 18, 68, bronze, 3);
        DrawLine(texture, 30, 74, 24, 64, leather, 2);
        DrawLine(texture, 97, 34, 91, 24, bronze, 3);
        DrawLine(texture, 90, 39, 84, 29, leather, 2);
        DrawLine(texture, 37, 69, 31, 59, leather, 2);
    }

    private static void DrawSubtleInlays(Texture2D texture, Color bronze)
    {
        DrawLine(texture, 35, 72, 29, 62, bronze, 1);
        DrawLine(texture, 48, 65, 42, 55, bronze, 1);
        DrawLine(texture, 86, 44, 80, 34, bronze, 1);
        DrawLine(texture, 96, 38, 90, 28, bronze, 1);
        DrawLine(texture, 57, 61, 66, 56, bronze, 1);
        DrawLine(texture, 70, 53, 78, 48, bronze, 1);
    }

    private static void DrawRaisedMouthpiece(Texture2D texture, Color bone, Color hole)
    {
        DrawLine(texture, 17, 82, 35, 72, bone, 9);
        DrawLine(texture, 20, 78, 34, 70, hole, 3);
        DrawLine(texture, 27, 71, 37, 66, bone, 3);
    }

    private static void DrawMouthNotch(Texture2D texture, Color color)
    {
        DrawLine(texture, 18, 80, 26, 72, color, 3);
        DrawCircle(texture, 28, 72, 2, color);
    }

    private static void DrawRuneMarks(Texture2D texture, Color color)
    {
        DrawLine(texture, 34, 77, 36, 70, color, 1);
        DrawLine(texture, 36, 70, 41, 73, color, 1);
        DrawLine(texture, 69, 50, 70, 43, color, 1);
        DrawLine(texture, 70, 43, 75, 46, color, 1);
        DrawLine(texture, 78, 51, 81, 45, color, 1);
        DrawLine(texture, 81, 45, 85, 48, color, 1);
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

    private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    SetPixel(texture, x, y, color);
                }
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
