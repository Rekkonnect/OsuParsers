using Garyon.Extensions;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace OsuParsers.Helpers;

internal static class ParseHelper
{
    public static FileSections GetCurrentSection(string line)
    {
        FileSections parsedSection = FileSections.None;
        Enum.TryParse(line.Trim(new char[] { '[', ']' }), true, out parsedSection);
        return parsedSection;
    }

    public static CurveType GetCurveType(char c)
    {
        switch (c)
        {
            case 'C':
                return CurveType.Catmull;
            case 'B':
                return CurveType.Bezier;
            case 'L':
                return CurveType.Linear;
            case 'P':
                return CurveType.PerfectCurve;
            default:
                return CurveType.PerfectCurve;
        }
    }

    public static List<Vector2> GetSliderPoints(string[] segments)
    {
        List<Vector2> sliderPoints = new List<Vector2>();
        foreach (string segmentPos in segments.Skip(1))
        {
            string[] positionTokens = segmentPos.Split(':');
            if (positionTokens.Length == 2)
            {
                var x = Convert.ToInt32(positionTokens[0], CultureInfo.InvariantCulture);
                var y = Convert.ToInt32(positionTokens[1], CultureInfo.InvariantCulture);
                sliderPoints.Add(new Vector2(x, y));
            }
        }
        return sliderPoints;
    }

    public static Color ParseColour(ReadOnlySpan<char> line)
    {
        const char delimiter = ',';
        line.SplitOnce(delimiter, out var r, out var gba);
        gba.SplitOnce(delimiter, out var g, out var ba);
        bool hasAlpha = ba.SplitOnce(delimiter, out var b, out var a);

        return Color.FromArgb(
            hasAlpha ? a.ParseInt32() : 255,
            r.ParseInt32(),
            g.ParseInt32(),
            b.ParseInt32());
    }

    public static bool IsLineValid(string line, FileSections currentSection)
    {
        switch (currentSection)
        {
            case FileSections.Format:
                return line.Contains("osu file format v", StringComparison.OrdinalIgnoreCase);
            case FileSections.General:
            case FileSections.Editor:
            case FileSections.Metadata:
            case FileSections.Difficulty:
            case FileSections.Fonts:
            case FileSections.Mania:
                return line.Contains(':');
            case FileSections.Events:
            case FileSections.TimingPoints:
            case FileSections.HitObjects:
                return line.Contains(',');
            case FileSections.Colours:
            case FileSections.CatchTheBeat:
                return line.Contains(',') && line.Contains(':');
            default: return false;
        }
    }

    public static bool ToBool(this ReadOnlySpan<char> value)
    {
        value = value.Trim();
        return value is "1"
            || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static float ToFloat(this ReadOnlySpan<char> value)
    {
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public static double ToDouble(this ReadOnlySpan<char> value)
    {
        return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
