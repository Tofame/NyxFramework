using NyxRender;

namespace NyxDrawer.Appearance;

/// <summary>
/// Port of WosClient <c>Color::getOutfitColor</c> — Nyx outfit palette index to RGB.
///
/// <b>Palette structure:</b> 19 hue steps × 7 saturation/brightness rows = 133 colours (0–132).
/// Indices where <c>index % 19 == 0</c> are grayscale; others are coloured.
///
/// <b>HSI-to-RGB conversion:</b> hue is mapped via a 6-segment piecewise function
/// (each segment covers 60° of the hue wheel).  Saturation scales the chroma between
/// the pure hue and a desaturated component.  Brightness scales the final value.
///
/// <b>Saturation/Brightness table (7 rows):</b>
/// Row 0: sat=0.25 bri=1.00 | Row 1: sat=0.25 bri=0.75 | Row 2: sat=0.50 bri=0.75 |
/// Row 3: sat=0.67 bri=0.75 | Row 4: sat=1.00 bri=1.00 | Row 5: sat=1.00 bri=0.75 |
/// Row 6: sat=1.00 bri=0.50
/// </summary>
public static class OutfitColor
{
    private const int HsiSiValues = 7;
    private const int HsiHSteps = 19;

	private static readonly Color[] ColorsTable;

	static OutfitColor()
	{
		ColorsTable = new Color[HsiHSteps * HsiSiValues];
		for (var i = 0; i < ColorsTable.Length; i++)
		{
			ColorsTable[i] = ComputeFromIndex(i);
		}
	}

	public static Color FromIndex(int color)
	{
		if (color < 0 || color >= ColorsTable.Length)
			return ColorsTable[0];
		return ColorsTable[color];
	}

    private static Color ComputeFromIndex(int color)
    {
        if (color < 0)
            color = 0;
        if (color >= HsiHSteps * HsiSiValues)
            color = 0;

        float hueFraction, saturation, brightness;
        if (color % HsiHSteps != 0)
        {
            hueFraction = color % HsiHSteps / 18f;
            saturation = 1;
            brightness = 1;

            switch (color / HsiHSteps)
            {
                case 0: saturation = 0.25f; brightness = 1.00f; break;
                case 1: saturation = 0.25f; brightness = 0.75f; break;
                case 2: saturation = 0.50f; brightness = 0.75f; break;
                case 3: saturation = 0.667f; brightness = 0.75f; break;
                case 4: saturation = 1.00f; brightness = 1.00f; break;
                case 5: saturation = 1.00f; brightness = 0.75f; break;
                default: saturation = 1.00f; brightness = 0.50f; break;
            }
        }
        else
        {
            hueFraction = 0;
            saturation = 0;
            brightness = 1f - (float)color / HsiHSteps / HsiSiValues;
        }

        if (brightness == 0)
            return new Color(0, 0, 0);

        if (saturation == 0)
        {
            var g = (byte)(brightness * 255);
            return new Color(g, g, g);
        }

        float red, green, blue;
        if (hueFraction < 1f / 6f)
        {
            red = brightness;
            blue = brightness * (1 - saturation);
            green = blue + (brightness - blue) * 6 * hueFraction;
        }
        else if (hueFraction < 2f / 6f)
        {
            green = brightness;
            blue = brightness * (1 - saturation);
            red = green - (brightness - blue) * (6 * hueFraction - 1);
        }
        else if (hueFraction < 3f / 6f)
        {
            green = brightness;
            red = brightness * (1 - saturation);
            blue = red + (brightness - red) * (6 * hueFraction - 2);
        }
        else if (hueFraction < 4f / 6f)
        {
            blue = brightness;
            red = brightness * (1 - saturation);
            green = blue - (brightness - red) * (6 * hueFraction - 3);
        }
        else if (hueFraction < 5f / 6f)
        {
            blue = brightness;
            green = brightness * (1 - saturation);
            red = green + (brightness - green) * (6 * hueFraction - 4);
        }
        else
        {
            red = brightness;
            green = brightness * (1 - saturation);
            blue = red - (brightness - green) * (6 * hueFraction - 5);
        }

        return new Color((byte)(red * 255), (byte)(green * 255), (byte)(blue * 255));
    }
}
