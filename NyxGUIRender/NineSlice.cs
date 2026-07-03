using System.Numerics;
using NyxGui;
using NyxGuiRender.Gl;

namespace NyxGuiRender;

/// <summary>
/// Implements NyxClient-style 9-slice / bordered-image drawing.
///
/// The source image is divided into 9 regions by border insets (top, bottom, left, right):
/// 4 corners drawn 1:1, 4 edges repeated along one axis, and a center repeated in both axes.
/// This allows scalable UI borders with fixed-size corners.
/// </summary>
internal static class NineSlice
{
    /// <summary>
    /// NyxClient <c>UIWidget::drawImage</c> bordered path — per-edge source insets, 1:1 on screen, tiles repeated.
    /// </summary>
    public static void Draw(
        GuiSpriteBatch batch,
        GuiTexture tex,
        NyxRect dest,
        NyxRect clip,
        NyxImageBorders borders,
        GuiColor tint)
    {
        if (dest.Width <= 0 || dest.Height <= 0 || clip.Width <= 0 || clip.Height <= 0 || !borders.HasAny)
            return;

        var top = borders.Top;
        var bottom = borders.Bottom;
        var left = borders.Left;
        var right = borders.Right;

        var sx = clip.X;
        var sy = clip.Y;
        var sw = clip.Width;
        var sh = clip.Height;

        var topLeftCorner = new NyxRect(sx, sy, left, top);
        var topRightCorner = new NyxRect(sx + sw - right, sy, right, top);
        var bottomLeftCorner = new NyxRect(sx, sy + sh - bottom, left, bottom);
        var bottomRightCorner = new NyxRect(sx + sw - right, sy + sh - bottom, right, bottom);
        var topBorder = new NyxRect(sx + left, sy, Math.Max(0, sw - left - right), top);
        var bottomBorder = new NyxRect(sx + left, sy + sh - bottom, Math.Max(0, sw - left - right), bottom);
        var leftBorder = new NyxRect(sx, sy + top, left, Math.Max(0, sh - top - bottom));
        var rightBorder = new NyxRect(sx + sw - right, sy + top, right, Math.Max(0, sh - top - bottom));
        var center = new NyxRect(sx + left, sy + top, Math.Max(0, sw - left - right), Math.Max(0, sh - top - bottom));

        var centerW = Math.Max(0, dest.Width - left - right);
        var centerH = Math.Max(0, dest.Height - top - bottom);

        var dx = dest.X;
        var dy = dest.Y;

        if (centerW > 0 && centerH > 0)
            DrawRepeated(batch, tex, new NyxRect(dx + left, dy + top, centerW, centerH), center, tint);

        DrawRepeated(batch, tex, new NyxRect(dx, dy, left, top), topLeftCorner, tint);
        DrawRepeated(batch, tex, new NyxRect(dx + left, dy, centerW, top), topBorder, tint);
        DrawRepeated(batch, tex, new NyxRect(dx + left + centerW, dy, right, top), topRightCorner, tint);
        DrawRepeated(batch, tex, new NyxRect(dx, dy + top, left, centerH), leftBorder, tint);
        DrawRepeated(batch, tex, new NyxRect(dx + left + centerW, dy + top, right, centerH), rightBorder, tint);
        DrawRepeated(batch, tex, new NyxRect(dx, dy + top + centerH, left, bottom), bottomLeftCorner, tint);
        DrawRepeated(batch, tex, new NyxRect(dx + left, dy + top + centerH, centerW, bottom), bottomBorder, tint);
        DrawRepeated(batch, tex, new NyxRect(dx + left + centerW, dy + top + centerH, right, bottom), bottomRightCorner, tint);
    }

    /// <summary>NyxClient <c>CoordsBuffer::addRepeatedRects</c> — tile <paramref name="src"/> across <paramref name="dest"/>.</summary>
    private static void DrawRepeated(
        GuiSpriteBatch batch,
        GuiTexture tex,
        NyxRect dest,
        NyxRect src,
        GuiColor tint)
    {
        if (dest.Width <= 0 || dest.Height <= 0 || src.Width <= 0 || src.Height <= 0)
            return;

        for (var y = dest.Y; y < dest.Bottom; y += src.Height)
        {
            var partialDestH = Math.Min(src.Height, dest.Bottom - y);
            var partialSrcY = src.Y;
            var partialSrcH = src.Height;
            if (partialDestH < src.Height)
                partialSrcH = partialDestH;

            for (var x = dest.X; x < dest.Right; x += src.Width)
            {
                var partialDestW = Math.Min(src.Width, dest.Right - x);
                var partialSrcX = src.X;
                var partialSrcW = src.Width;
                if (partialDestW < src.Width)
                    partialSrcW = partialDestW;

                DrawStretch(
                    batch,
                    tex,
                    new NyxRect(x, y, partialDestW, partialDestH),
                    new NyxRect(partialSrcX, partialSrcY, partialSrcW, partialSrcH),
                    tint);
            }
        }
    }

    private static void DrawStretch(
        GuiSpriteBatch batch,
        GuiTexture tex,
        NyxRect dest,
        NyxRect src,
        GuiColor tint)
    {
        if (dest.Width <= 0 || dest.Height <= 0 || src.Width <= 0 || src.Height <= 0)
            return;

        var tw = tex.Width;
        var th = tex.Height;
        var u0 = src.X / (float)tw;
        var v0 = src.Y / (float)th;
        var u1 = src.Right / (float)tw;
        var v1 = src.Bottom / (float)th;
        batch.Draw(
            tex,
            new Vector2(dest.X, dest.Y),
            new Vector2(dest.Width, dest.Height),
            tint,
            new Vector2(u0, v0),
            new Vector2(u1, v1));
    }
}
