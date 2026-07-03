using System;
using System.Collections.Generic;
using NyxGui;
using Sandbox.Items;

namespace Sandbox.UI.Inventory;

public static class ItemTooltip
{
	private const int Pad = 8;
	private const int LineGap = 4;
	private const int TitleHeight = 16;
	private const int AttrHeight = 14;
	private const int DescHeight = 14;
	private const int MaxDescWidth = 220;

	public static void GetTooltipInfo(
		INyxGuiPainter painter,
		Item item,
		NyxFontStyle? font,
		out string title,
		out List<string> attrs,
		out List<string> descLines,
		out int tooltipW,
		out int tooltipH)
	{
		title = string.Empty;
		attrs = new List<string>();
		descLines = new List<string>();
		tooltipW = 0;
		tooltipH = 0;

		if (item.IsEmpty) return;
		var itemType = item.GetItemType();
		if (itemType.IsNone) return;

		title = item.Count > 1 && itemType.Stackable
			? $"{itemType.GetDisplayLabel()} ({item.Count})"
			: itemType.GetDisplayLabel();

		if (itemType.Attack > 0)
			attrs.Add($"Attack: {itemType.Attack}");
		if (itemType.Armor > 0)
			attrs.Add($"Armor: {itemType.Armor}");
		if (itemType.RequiredEquipmentSlot.HasValue)
			attrs.Add($"Slot: {itemType.RequiredEquipmentSlot.Value}");
		if (itemType.IsContainer)
			attrs.Add($"Capacity: {itemType.ContainerCapacity} slots");
		if (itemType.Weight > 0)
			attrs.Add($"Weight: {itemType.Weight:F2} oz");

		if (!string.IsNullOrEmpty(itemType.Description))
		{
			var rawLines = itemType.Description.Split('\n');
			foreach (var rawLine in rawLines)
			{
				descLines.AddRange(WordWrap(painter, rawLine, font, MaxDescWidth));
			}
		}

		// Measure dimensions
		painter.MeasureText(title, font, out int maxW, out _);
		maxW = Math.Max(maxW, 120);

		foreach (var attr in attrs)
		{
			painter.MeasureText(attr, font, out int w, out _);
			if (w > maxW) maxW = w;
		}

		foreach (var line in descLines)
		{
			painter.MeasureText(line, font, out int w, out _);
			if (w > maxW) maxW = w;
		}

		tooltipW = maxW + Pad * 2;

		int totalH = Pad * 2;
		totalH += TitleHeight;

		if (attrs.Count > 0)
		{
			totalH += LineGap;
			totalH += attrs.Count * AttrHeight + (attrs.Count - 1) * LineGap;
		}

		if (descLines.Count > 0)
		{
			totalH += LineGap + 8; // gap before separator + separator line height + gap after
			totalH += descLines.Count * DescHeight + (descLines.Count - 1) * LineGap;
		}

		tooltipH = totalH;
	}

	public static void Paint(
		INyxGuiPainter painter,
		int x,
		int y,
		string title,
		List<string> attrs,
		List<string> descLines,
		int tooltipW,
		int tooltipH,
		NyxFontStyle? font)
	{
		var bounds = new NyxRect(x, y, tooltipW, tooltipH);

		// Fill background
		painter.FillRect(bounds, NyxColor.FromRgb(15, 15, 18));
		// Draw red border of width 1
		painter.DrawRect(bounds, NyxColor.FromRgb(255, 0, 0), 1);

		int currentY = y + Pad;
		int innerW = tooltipW - Pad * 2;

		// Draw Title (centered)
		var titleRect = new NyxRect(x + Pad, currentY, innerW, TitleHeight);
		painter.DrawText(titleRect, title, NyxTextAlign.Center, NyxColor.FromRgb(255, 204, 0), font);
		currentY += TitleHeight;

		// Draw Attributes (left-aligned)
		if (attrs.Count > 0)
		{
			currentY += LineGap;
			foreach (var attr in attrs)
			{
				var attrRect = new NyxRect(x + Pad, currentY, innerW, AttrHeight);
				painter.DrawText(attrRect, attr, NyxTextAlign.TopLeft, NyxColor.FromRgb(180, 180, 200), font);
				currentY += AttrHeight + LineGap;
			}
			currentY -= LineGap;
		}

		// Draw Separator & Description (centered description)
		if (descLines.Count > 0)
		{
			currentY += LineGap + 4;
			var sepRect = new NyxRect(x + Pad, currentY, innerW, 1);
			painter.FillRect(sepRect, NyxColor.FromRgb(100, 30, 30));
			currentY += 5;

			foreach (var line in descLines)
			{
				var descRect = new NyxRect(x + Pad, currentY, innerW, DescHeight);
				painter.DrawText(descRect, line, NyxTextAlign.Center, NyxColor.FromRgb(210, 210, 190), font);
				currentY += DescHeight + LineGap;
			}
		}
	}

	private static List<string> WordWrap(INyxGuiPainter painter, string text, NyxFontStyle? font, int maxWidth)
	{
		var result = new List<string>();
		var words = text.Split(' ');
		var currentLine = "";

		foreach (var word in words)
		{
			var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
			painter.MeasureText(testLine, font, out int w, out _);
			if (w > maxWidth && !string.IsNullOrEmpty(currentLine))
			{
				result.Add(currentLine);
				currentLine = word;
			}
			else
			{
				currentLine = testLine;
			}
		}

		if (!string.IsNullOrEmpty(currentLine))
		{
			result.Add(currentLine);
		}

		return result;
	}
}
