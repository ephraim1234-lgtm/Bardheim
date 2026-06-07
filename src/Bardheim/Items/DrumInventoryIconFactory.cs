using UnityEngine;

namespace Bardheim.Items;

internal static class DrumInventoryIconFactory
{
    private const int Size = 128;

    public static Sprite Create()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "Bardheim_GeneratedDrumInventoryIcon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Clear(texture, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        DrawDrum(texture);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, Size, Size),
            new Vector2(0.5f, 0.5f),
            Size);
    }

    private static void DrawDrum(Texture2D texture)
    {
        var shadow = new Color(0.035f, 0.025f, 0.018f, 0.65f);
        var darkWood = new Color(0.18f, 0.08f, 0.035f, 1.0f);
        var midWood = new Color(0.42f, 0.22f, 0.09f, 1.0f);
        var lightWood = new Color(0.68f, 0.39f, 0.16f, 1.0f);
        var hide = new Color(0.72f, 0.58f, 0.36f, 1.0f);
        var hideLight = new Color(0.88f, 0.75f, 0.48f, 1.0f);
        var bronze = new Color(0.43f, 0.31f, 0.15f, 1.0f);
        var bronzeLight = new Color(0.68f, 0.52f, 0.25f, 1.0f);
        var leather = new Color(0.23f, 0.11f, 0.045f, 1.0f);
        var redPigment = new Color(0.45f, 0.09f, 0.04f, 1.0f);

        DrawBarrel(texture, 67, 31, 99, 96, darkWood);
        DrawEllipse(texture, 70, 68, 36, 36, shadow);
        DrawEllipse(texture, 62, 64, 41, 39, darkWood);
        DrawEllipse(texture, 62, 64, 35, 33, midWood);
        DrawEllipse(texture, 62, 64, 30, 29, hide);
        DrawEllipse(texture, 55, 54, 15, 11, hideLight);
        DrawSideHandles(texture, leather, bronze);
        DrawEdgeTabs(texture, leather);
        DrawShellBraces(texture, leather, bronze);
        DrawEllipseOutline(texture, 62, 64, 40, 38, darkWood, 6);
        DrawEllipseOutline(texture, 62, 64, 34, 32, lightWood, 3);
        DrawEllipseOutline(texture, 62, 64, 26, 25, leather, 2);
        DrawWeatheredHide(texture, leather, bronze);

        for (var index = 0; index < 14; index++)
        {
            var angle = index * Mathf.PI * 2.0f / 14.0f;
            var outerX = 62 + Mathf.RoundToInt(Mathf.Cos(angle) * 37.0f);
            var outerY = 64 + Mathf.RoundToInt(Mathf.Sin(angle) * 35.0f);
            var innerX = 62 + Mathf.RoundToInt(Mathf.Cos(angle) * 28.0f);
            var innerY = 64 + Mathf.RoundToInt(Mathf.Sin(angle) * 27.0f);
            DrawLine(texture, outerX, outerY, innerX, innerY, leather, 2);
            DrawCircle(texture, outerX, outerY, 2, bronzeLight);
        }

        for (var index = 0; index < 6; index++)
        {
            var angle = index * Mathf.PI * 2.0f / 6.0f + 0.25f;
            DrawLine(
                texture,
                62 + Mathf.RoundToInt(Mathf.Cos(angle) * 9.0f),
                64 + Mathf.RoundToInt(Mathf.Sin(angle) * 8.0f),
                62 + Mathf.RoundToInt(Mathf.Cos(angle) * 22.0f),
                64 + Mathf.RoundToInt(Mathf.Sin(angle) * 20.0f),
                redPigment,
                2);
        }

        DrawEllipseOutline(texture, 62, 64, 19, 18, redPigment, 2);
        DrawLine(texture, 44, 64, 80, 64, redPigment, 2);
        DrawLine(texture, 62, 45, 62, 83, redPigment, 2);
        DrawLine(texture, 47, 82, 77, 46, redPigment, 2);
        DrawLine(texture, 47, 46, 77, 82, redPigment, 2);
        DrawLine(texture, 50, 77, 68, 48, leather, 1);
        DrawLine(texture, 74, 77, 56, 48, leather, 1);
        DrawLine(texture, 52, 29, 84, 38, darkWood, 5);
        DrawLine(texture, 55, 31, 81, 38, bronze, 2);
    }

    private static void DrawSideHandles(Texture2D texture, Color leather, Color bronze)
    {
        DrawLine(texture, 29, 51, 20, 59, leather, 3);
        DrawLine(texture, 20, 59, 20, 73, leather, 4);
        DrawLine(texture, 20, 73, 29, 81, leather, 3);
        DrawLine(texture, 93, 51, 102, 59, leather, 3);
        DrawLine(texture, 102, 59, 102, 73, leather, 4);
        DrawLine(texture, 102, 73, 93, 81, leather, 3);
        DrawRect(texture, 27, 49, 6, 8, bronze);
        DrawRect(texture, 27, 78, 6, 8, bronze);
        DrawRect(texture, 90, 49, 6, 8, bronze);
        DrawRect(texture, 90, 78, 6, 8, bronze);
    }

    private static void DrawEdgeTabs(Texture2D texture, Color leather)
    {
        for (var index = 0; index < 18; index++)
        {
            var angle = index * Mathf.PI * 2.0f / 18.0f + 0.15f;
            var x0 = 62 + Mathf.RoundToInt(Mathf.Cos(angle) * 36.0f);
            var y0 = 64 + Mathf.RoundToInt(Mathf.Sin(angle) * 34.0f);
            var x1 = 62 + Mathf.RoundToInt(Mathf.Cos(angle) * 42.0f);
            var y1 = 64 + Mathf.RoundToInt(Mathf.Sin(angle) * 39.0f);
            DrawLine(texture, x0, y0, x1, y1, leather, 2);
        }
    }

    private static void DrawWeatheredHide(Texture2D texture, Color leather, Color bronze)
    {
        DrawLine(texture, 42, 48, 56, 53, bronze, 3);
        DrawLine(texture, 68, 78, 84, 72, leather, 3);
        DrawLine(texture, 47, 85, 62, 88, bronze, 2);
        DrawLine(texture, 73, 45, 86, 52, leather, 2);
        DrawLine(texture, 43, 55, 53, 51, bronze, 1);
        DrawLine(texture, 43, 75, 57, 80, bronze, 1);
        DrawLine(texture, 74, 52, 84, 59, leather, 1);
        DrawLine(texture, 73, 82, 84, 76, bronze, 1);
        DrawLine(texture, 55, 42, 65, 39, leather, 1);
        DrawLine(texture, 50, 92, 66, 89, leather, 1);
        DrawCircle(texture, 52, 58, 1, bronze);
        DrawCircle(texture, 72, 70, 1, leather);
        DrawCircle(texture, 59, 80, 1, bronze);
    }

    private static void DrawShellBraces(Texture2D texture, Color leather, Color bronze)
    {
        DrawLine(texture, 82, 37, 100, 54, leather, 2);
        DrawLine(texture, 100, 54, 84, 92, leather, 2);
        DrawLine(texture, 89, 38, 101, 69, bronze, 1);
        DrawLine(texture, 101, 69, 90, 93, bronze, 1);
        DrawLine(texture, 75, 33, 94, 47, leather, 2);
        DrawLine(texture, 94, 86, 75, 98, leather, 2);
    }

    private static void DrawBarrel(Texture2D texture, int left, int top, int right, int bottom, Color color)
    {
        for (var y = top; y <= bottom; y++)
        {
            var t = (y - top) / (float)(bottom - top);
            var width = Mathf.Sin(t * Mathf.PI) * 9.0f;
            var inset = Mathf.RoundToInt(6.0f - width);
            DrawLine(texture, left + inset, y, right - inset, y, color, 1);
        }
    }

    private static void DrawEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                var nx = (x - centerX) / (float)radiusX;
                var ny = (y - centerY) / (float)radiusY;
                if (nx * nx + ny * ny <= 1.0f)
                {
                    SetPixel(texture, x, y, color);
                }
            }
        }
    }

    private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        DrawEllipse(texture, centerX, centerY, radius, radius, color);
    }

    private static void DrawEllipseOutline(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color, int thickness)
    {
        for (var offset = 0; offset < thickness; offset++)
        {
            for (var angle = 0; angle < 360; angle += 2)
            {
                var radians = angle * Mathf.Deg2Rad;
                SetPixel(
                    texture,
                    centerX + Mathf.RoundToInt(Mathf.Cos(radians) * (radiusX - offset)),
                    centerY + Mathf.RoundToInt(Mathf.Sin(radians) * (radiusY - offset)),
                    color);
            }
        }
    }

    private static void DrawArcBand(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color, int thickness)
    {
        for (var offset = 0; offset < thickness; offset++)
        {
            for (var angle = 18; angle <= 162; angle += 2)
            {
                var radians = angle * Mathf.Deg2Rad;
                SetPixel(
                    texture,
                    centerX + Mathf.RoundToInt(Mathf.Cos(radians) * (radiusX - offset)),
                    centerY + Mathf.RoundToInt(Mathf.Sin(radians) * (radiusY - offset)),
                    color);
            }
        }
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
