using Garyon.Extensions;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Beatmaps.Objects.Catch;
using OsuParsers.Beatmaps.Objects.Mania;
using OsuParsers.Beatmaps.Objects.Taiko;
using OsuParsers.Beatmaps.Sections.Events;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;
using OsuParsers.Enums.Storyboards;
using OsuParsers.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OsuParsers.Decoders;

public static class BeatmapDecoder
{
    private static Beatmap Beatmap;
    private static FileSections currentSection = FileSections.None;
    private static List<string> sbLines = new List<string>();

    /// <summary>
    /// Parses .osu file.
    /// </summary>
    /// <param name="fileInfo">Path to the .osu file.</param>
    /// <returns>A usable beatmap.</returns>
    public static Beatmap Decode(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException();
        }

        return Decode(File.ReadAllLines(fileInfo.FullName));
    }

    /// <summary>
    /// Parses .osu file.
    /// </summary>
    /// <param name="path">Path to the .osu file.</param>
    /// <returns>A usable beatmap.</returns>
    public static Beatmap Decode(string path)
    {
        if (File.Exists(path))
            return Decode(File.ReadAllLines(path));
        else
            throw new FileNotFoundException();
    }

    public static Beatmap DecodeMultilineString(string contents)
    {
        var lines = ReadAllLines(contents);
        return Decode(lines);
    }

    private static IEnumerable<string> ReadAllLines(string content)
    {
        var reader = new StringReader(content);
        var lines = new List<string>();
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
                break;

            lines.Add(line);
        }

        return lines;
    }

    /// <summary>
    /// Parses .osu file.
    /// </summary>
    /// <param name="lines">Array of text lines containing beatmap data.</param>
    /// <returns>A usable beatmap.</returns>
    public static Beatmap Decode(IEnumerable<string> lines)
    {
        Beatmap = new Beatmap();
        currentSection = FileSections.Format;
        sbLines.Clear();

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
            {
                var parsedSection = ParseHelper.GetCurrentSection(line);
                if (parsedSection is not FileSections.None)
                    currentSection = parsedSection;
                else if (ParseHelper.IsLineValid(line, currentSection))
                    ParseLine(line);
            }
        }

        Beatmap.EventsSection.Storyboard = StoryboardDecoder.Decode(sbLines.ToArray());

        Beatmap.GeneralSection.CirclesCount = Beatmap.HitObjects.Count(c => c is ICircleHitObject);
        Beatmap.GeneralSection.SlidersCount = Beatmap.HitObjects.Count(c => c is ISliderHitObject);
        Beatmap.GeneralSection.SpinnersCount = Beatmap.HitObjects.Count(c => c is ISpinnerHitObject);

        Beatmap.GeneralSection.Length = Beatmap.HitObjects.LastOrDefault()?.EndTime ?? 0;

        return Beatmap;
    }

    /// <summary>
    /// Parses .osu file.
    /// </summary>
    /// <param name="stream">Stream containing beatmap data.</param>
    /// <returns>A usable beatmap.</returns>
    public static Beatmap Decode(Stream stream) => Decode(stream.ReadAllLines());

    private static void ParseLine(string line)
    {
        switch (currentSection)
        {
            case FileSections.Format:
                Beatmap.Version = Convert.ToInt32(line.Split(new string[] { "osu file format v" }, StringSplitOptions.None)[1]);
                break;
            case FileSections.General:
                ParseGeneral(line);
                break;
            case FileSections.Editor:
                ParseEditor(line);
                break;
            case FileSections.Metadata:
                ParseMetadata(line);
                break;
            case FileSections.Difficulty:
                ParseDifficulty(line);
                break;
            case FileSections.Events:
                ParseEvents(line);
                break;
            case FileSections.TimingPoints:
                ParseTimingPoints(line);
                break;
            case FileSections.Colours:
                ParseColours(line);
                break;
            case FileSections.HitObjects:
                ParseHitObjects(line);
                break;
        }
    }

    private static void ParseGeneral(string line)
    {
        line.AsSpan().SplitOnce(':', out var variable, out var value);
        value = value.Trim();

        switch (variable)
        {
            case "AudioLeadIn":
                Beatmap.GeneralSection.AudioLeadIn = value.ParseInt32();
                return;
            case "PreviewTime":
                Beatmap.GeneralSection.PreviewTime = value.ParseInt32();
                return;
            case "Countdown":
                Beatmap.GeneralSection.Countdown = ParseHelper.ToBool(value);
                return;
            case "StackLeniency":
                Beatmap.GeneralSection.StackLeniency = ParseHelper.ToDouble(value);
                return;
            case "Mode":
                int modeId = value.ParseInt32();
                Beatmap.GeneralSection.ModeId = modeId;
                Beatmap.GeneralSection.Mode = (Ruleset)modeId;
                return;
            case "LetterboxInBreaks":
                Beatmap.GeneralSection.LetterboxInBreaks = ParseHelper.ToBool(value);
                return;
            case "WidescreenStoryboard":
                Beatmap.GeneralSection.WidescreenStoryboard = ParseHelper.ToBool(value);
                return;
            case "StoryFireInFront":
                Beatmap.GeneralSection.StoryFireInFront = ParseHelper.ToBool(value);
                return;
            case "SpecialStyle":
                Beatmap.GeneralSection.SpecialStyle = ParseHelper.ToBool(value);
                return;
            case "EpilepsyWarning":
                Beatmap.GeneralSection.EpilepsyWarning = ParseHelper.ToBool(value);
                return;
            case "UseSkinSprites":
                Beatmap.GeneralSection.UseSkinSprites = ParseHelper.ToBool(value);
                return;
        }

        var valueString = value.ToString();

        switch (variable)
        {
            case "AudioFilename":
                Beatmap.GeneralSection.AudioFilename = valueString.Trim();
                break;
            case "SampleSet":
                Beatmap.GeneralSection.SampleSet = Enum.Parse<SampleSet>(valueString);
                break;
        }
    }

    private static void ParseEditor(string line)
    {
        line.AsSpan().SplitOnce(':', out var variable, out var value);
        value = value.Trim();

        // quickly rule out the need to convert the span into a new string
        switch (variable)
        {
            case "DistanceSpacing":
                Beatmap.EditorSection.DistanceSpacing = ParseHelper.ToDouble(value);
                return;
            case "BeatDivisor":
                Beatmap.EditorSection.BeatDivisor = value.ParseInt32();
                return;
            case "GridSize":
                Beatmap.EditorSection.GridSize = value.ParseInt32();
                return;
            case "TimelineZoom":
                Beatmap.EditorSection.TimelineZoom = ParseHelper.ToFloat(value);
                return;
        }

        var valueString = value.ToString();

        const int emptyBookmarkValue = int.MinValue;

        switch (variable)
        {
            case "Bookmarks":
                Beatmap.EditorSection.Bookmarks = valueString
                    .AsSpan()
                    .SplitSelect(',', ParseNonEmpty)
                    .Where(s => s > emptyBookmarkValue)
                    .ToArray();
                break;
        }

        static int ParseNonEmpty(ReadOnlySpan<char> s)
        {
            if (s.Length is 0)
                return emptyBookmarkValue;

            return s.ParseInt32();
        }
    }

    private static readonly char[] _metadataTagDelimiters = [',', ' '];

    private static void ParseMetadata(string line)
    {
        line.AsSpan().SplitOnce(':', out var variable, out var value);
        value = value.Trim();

        // quickly rule out the need to convert the span into a new string
        switch (variable)
        {
            case "BeatmapID":
                Beatmap.MetadataSection.BeatmapID = value.ParseInt32();
                return;
            case "BeatmapSetID":
                Beatmap.MetadataSection.BeatmapSetID = value.ParseInt32();
                return;
        }

        var valueString = value.ToString();

        switch (variable)
        {
            case "Title":
                Beatmap.MetadataSection.Title = valueString;
                break;
            case "TitleUnicode":
                Beatmap.MetadataSection.TitleUnicode = valueString;
                break;
            case "Artist":
                Beatmap.MetadataSection.Artist = valueString;
                break;
            case "ArtistUnicode":
                Beatmap.MetadataSection.ArtistUnicode = valueString;
                break;
            case "Creator":
                Beatmap.MetadataSection.Creator = valueString;
                break;
            case "Version":
                Beatmap.MetadataSection.Version = valueString;
                break;
            case "Source":
                Beatmap.MetadataSection.Source = valueString;
                break;
            case "Tags":
                Beatmap.MetadataSection.Tags = valueString
                    .Split(
                        _metadataTagDelimiters,
                        StringSplitOptions.RemoveEmptyEntries);
                break;
        }
    }

    private static void ParseDifficulty(string line)
    {
        line.AsSpan().SplitOnce(':', out var variable, out var value);
        value = value.Trim();

        switch (variable)
        {
            case "HPDrainRate":
                Beatmap.DifficultySection.HPDrainRate = ParseHelper.ToFloat(value);
                break;
            case "CircleSize":
                Beatmap.DifficultySection.CircleSize = ParseHelper.ToFloat(value);
                break;
            case "OverallDifficulty":
                Beatmap.DifficultySection.OverallDifficulty = ParseHelper.ToFloat(value);
                break;
            case "ApproachRate":
                Beatmap.DifficultySection.ApproachRate = ParseHelper.ToFloat(value);
                break;
            case "SliderMultiplier":
                Beatmap.DifficultySection.SliderMultiplier = ParseHelper.ToDouble(value);
                break;
            case "SliderTickRate":
                Beatmap.DifficultySection.SliderTickRate = ParseHelper.ToDouble(value);
                break;
        }
    }

    private static void ParseEvents(string line)
    {
        var tokenContainer = new ParsedEventTokens();
        line.SplitToTokenStruct(',', ref tokenContainer);
        var tokens = tokenContainer.ParsedSpan();

        EventType eventType;

        if (Enum.TryParse(tokens[0].ToString(), out EventType e))
            eventType = e;
        else if (line.StartsWith(" ") || line.StartsWith("_"))
            eventType = EventType.StoryboardCommand;
        else
            return;

        switch (eventType)
        {
            case EventType.Background:
                Beatmap.EventsSection.BackgroundImage = tokens[2].Trim('"').ToString();
                break;
            case EventType.Video:
                Beatmap.EventsSection.Video = tokens[2].Trim('"').ToString();
                Beatmap.EventsSection.VideoOffset = tokens[1].Span.ParseInt32();
                break;
            case EventType.Break:
                Beatmap.EventsSection.Breaks.Add(
                    new BeatmapBreakEvent(
                        tokens[1].Span.ParseInt32(),
                        tokens[2].Span.ParseInt32()));
                break;
            case EventType.Sprite:
            case EventType.Animation:
            case EventType.Sample:
            case EventType.StoryboardCommand:
                sbLines.Add(line);
                break;
        }
    }

    private static void ParseTimingPoints(string line)
    {
        var tokenContainer = new ParsedTimingPointTokens();
        line.SplitToTokenStruct(',', ref tokenContainer);
        var tokens = tokenContainer.ParsedSpan();

        int offset = (int)ParseHelper.ToFloat(tokens[0].Span);
        double beatLength = ParseHelper.ToDouble(tokens[1].Span);
        var timeSignature = TimeSignature.SimpleQuadruple;
        var sampleSet = SampleSet.None;
        int customSampleSet = 0;
        int volume = 100;
        bool inherited = true;
        var effects = Effects.None;

        if (tokens.Length >= 3)
            timeSignature = (TimeSignature)int.Parse(tokens[2].Span);

        if (tokens.Length >= 4)
            sampleSet = (SampleSet)int.Parse(tokens[3].Span);

        if (tokens.Length >= 5)
            customSampleSet = int.Parse(tokens[4].Span);

        if (tokens.Length >= 6)
            volume = int.Parse(tokens[5].Span);

        if (tokens.Length >= 7)
            inherited = !ParseHelper.ToBool(tokens[6].Span);

        if (tokens.Length >= 8)
            effects = (Effects)int.Parse(tokens[7].Span);

        Beatmap.TimingPoints.Add(new TimingPoint
        {
            Offset = offset,
            BeatLength = beatLength,
            TimeSignature = timeSignature,
            SampleSet = sampleSet,
            CustomSampleSet = customSampleSet,
            Volume = volume,
            Inherited = inherited,
            Effects = effects
        });
    }

    private static void ParseColours(string line)
    {
        line.AsSpan().SplitOnce(':', out var variable, out var value);
        variable = variable.Trim();
        value = value.Trim();

        switch (variable)
        {
            case "SliderTrackOverride":
                Beatmap.ColoursSection.SliderTrackOverride = ParseHelper.ParseColour(value);
                break;
            case "SliderBorder":
                Beatmap.ColoursSection.SliderBorder = ParseHelper.ParseColour(value);
                break;
            default:
                Beatmap.ColoursSection.ComboColours.Add(ParseHelper.ParseColour(value));
                break;
        }
    }

    private static void ParseHitObjects(string line)
    {
        var tokenContainer = new ParsedHitObjectTokens();
        line.SplitToTokenStruct(',', ref tokenContainer);
        var tokens = tokenContainer.ParsedSpan();

        var position = new Vector2(
            ParseHelper.ToFloat(tokens[0].Span),
            ParseHelper.ToFloat(tokens[1].Span));

        int startTime = tokens[2].Span.ParseInt32();

        HitObjectType type = (HitObjectType)tokens[3].Span.ParseInt32();

        int comboOffset = (int)(type & HitObjectType.ComboOffset) >> 4;
        type &= ~HitObjectType.ComboOffset;

        bool isNewCombo = type.HasFlag(HitObjectType.NewCombo);
        type &= ~HitObjectType.NewCombo;

        HitSoundType hitSound = (HitSoundType)tokens[4].Span.ParseInt32();

        HitObject hitObject = null;

        var extrasContainer = new ParsedExtrasTokens();
        tokens.Last().SplitToTokenStruct(':', ref extrasContainer);
        var extrasSplit = extrasContainer.ParsedSpan();

        int extrasOffset = type.HasFlag(HitObjectType.Hold) ? 1 : 0;
        var extras = new Extras();
        if (tokens.Last().Span.Contains(':'))
        {
            var importantExtrasTokens = extrasSplit;
            if (extrasOffset > 0)
            {
                importantExtrasTokens = extrasSplit[extrasOffset..];
            }

            // too much ternary magic
            extras.SampleSet = (SampleSet)importantExtrasTokens[0].Span.ParseInt32();
            extras.AdditionSet = (SampleSet)importantExtrasTokens[1].Span.ParseInt32();
            extras.CustomIndex = importantExtrasTokens.Length > 2 ? importantExtrasTokens[2].Span.ParseInt32() : 0;
            extras.Volume = importantExtrasTokens.Length > 3 ? importantExtrasTokens[3].Span.ParseInt32() : 0;
            extras.SampleFileName = importantExtrasTokens.Length > 4 ? importantExtrasTokens[4].ToString() : string.Empty;
        }

        switch (type)
        {
            case HitObjectType.Circle:
            {
                switch (Beatmap.GeneralSection.Mode)
                {
                    case Ruleset.Standard:
                        hitObject = new HitCircle(
                            position, startTime, startTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                    case Ruleset.Taiko:
                        hitObject = new TaikoHit(
                            position, startTime, startTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                    case Ruleset.Fruits:
                        hitObject = new CatchFruit(
                            position, startTime, startTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                    case Ruleset.Mania:
                        hitObject = new ManiaNote(
                            position, startTime, startTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                }
                break;
            }
            case HitObjectType.Slider:
            {
                // still room for improvement --
                // this allocates an array and the underlying strings
                var splitSliderInfo = tokens[5].Span.SplitToStrings('|')
                    .ToArrayOrExisting();
                var curveType = ParseHelper.GetCurveType(splitSliderInfo[0][0]);
                var sliderPoints = ParseHelper.GetSliderPoints(splitSliderInfo);

                int repeats = tokens[6].Span.ParseInt32();
                double pixelLength = ParseHelper.ToDouble(tokens[7].Span);

                int endTime = MathHelper.CalculateEndTime(Beatmap, startTime, repeats, pixelLength);

                List<HitSoundType> edgeHitSounds = null;
                if (tokens.Length > 8 && tokens[8].Length > 0)
                {
                    edgeHitSounds = tokens[8].Span
                        .SplitSelect('|', s => (HitSoundType)s.ParseInt32())
                        .ToList();
                }

                List<Tuple<SampleSet, SampleSet>> edgeAdditions = null;
                if (tokens.Length > 9 && tokens[9].Length > 0)
                {
                    edgeAdditions = tokens[9].Span
                        .SplitSelect('|', s =>
                        {
                            s.SplitOnce(':', out var left, out var right);
                            var leftSet = (SampleSet)left.ParseInt32();
                            var rightSet = (SampleSet)right.ParseInt32();
                            return new Tuple<SampleSet, SampleSet>(leftSet, rightSet);
                        })
                        .ToList();
                }

                switch (Beatmap.GeneralSection.Mode)
                {
                    case Ruleset.Standard:
                    {
                        hitObject = new Slider(
                            position, startTime, endTime, hitSound, curveType,
                            sliderPoints, repeats, pixelLength, isNewCombo, comboOffset,
                            edgeHitSounds, edgeAdditions, extras);
                        break;
                    }
                    case Ruleset.Taiko:
                    {
                        hitObject = new TaikoDrumroll(
                            position, startTime, endTime, hitSound, curveType, sliderPoints,
                            repeats, pixelLength, edgeHitSounds, edgeAdditions, extras,
                            isNewCombo, comboOffset);
                        break;
                    }
                    case Ruleset.Fruits:
                    {
                        hitObject = new CatchJuiceStream(
                            position, startTime, endTime, hitSound, curveType, sliderPoints,
                            repeats, pixelLength, isNewCombo, comboOffset, edgeHitSounds,
                            edgeAdditions, extras);
                        break;
                    }
                    case Ruleset.Mania:
                    {
                        hitObject = new ManiaHoldNote(
                            position, startTime, endTime, hitSound, extras,
                            isNewCombo, comboOffset);
                        break;
                    }
                }
                break;
            }
            case HitObjectType.Spinner:
            {
                int endTime = tokens[5].Span.Trim().ParseInt32();

                switch (Beatmap.GeneralSection.Mode)
                {
                    case Ruleset.Standard:
                        hitObject = new Spinner(
                            position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                    case Ruleset.Taiko:
                        hitObject = new TaikoSpinner(
                            position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                    case Ruleset.Fruits:
                        hitObject = new CatchBananaRain(
                            position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset);
                        break;
                }
                break;
            }
            case HitObjectType.Hold:
            {
                tokens[5].Span.SplitOnce(':', out var endTimeSpan, out var rest);
                int endTime = endTimeSpan.Trim().ParseInt32();
                hitObject = new ManiaHoldNote(
                    position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset);
                break;
            }
        }

        Beatmap.HitObjects.Add(hitObject);
    }
}

// token structs

#pragma warning disable IDE0032 // Use auto property
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0251 // Make member 'readonly'

[StructLayout(LayoutKind.Sequential)]
internal struct ParsedEventTokens : ITokenStruct
{
    private const int _length = 3;

    private int _parsedLength;

    private ReadOnlyMemory<char> _0;
    private ReadOnlyMemory<char> _1;
    private ReadOnlyMemory<char> _2;

    public readonly int MaxLength => _length;
    public int ParsedLength
    {
        get => _parsedLength;
        set => _parsedLength = value;
    }

    public Span<ReadOnlyMemory<char>> MaxLengthSpan()
    {
        return CreateSpan(_length);
    }
    public Span<ReadOnlyMemory<char>> ParsedSpan()
    {
        return CreateSpan(_parsedLength);
    }
    public Span<ReadOnlyMemory<char>> CreateSpan(int length)
    {
        return MemoryMarshal.CreateSpan(ref _0, length);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ParsedTimingPointTokens : ITokenStruct
{
    private const int _length = 8;

    private int _parsedLength;

    private ReadOnlyMemory<char> _0;
    private ReadOnlyMemory<char> _1;
    private ReadOnlyMemory<char> _2;
    private ReadOnlyMemory<char> _3;
    private ReadOnlyMemory<char> _4;
    private ReadOnlyMemory<char> _5;
    private ReadOnlyMemory<char> _6;
    private ReadOnlyMemory<char> _7;

    public readonly int MaxLength => _length;
    public int ParsedLength
    {
        get => _parsedLength;
        set => _parsedLength = value;
    }

    public Span<ReadOnlyMemory<char>> MaxLengthSpan()
    {
        return CreateSpan(_length);
    }
    public Span<ReadOnlyMemory<char>> ParsedSpan()
    {
        return CreateSpan(_parsedLength);
    }
    public Span<ReadOnlyMemory<char>> CreateSpan(int length)
    {
        return MemoryMarshal.CreateSpan(ref _0, length);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ParsedHitObjectTokens : ITokenStruct
{
    private const int _length = 10;

    private int _parsedLength;

    private ReadOnlyMemory<char> _0;
    private ReadOnlyMemory<char> _1;
    private ReadOnlyMemory<char> _2;
    private ReadOnlyMemory<char> _3;
    private ReadOnlyMemory<char> _4;
    private ReadOnlyMemory<char> _5;
    private ReadOnlyMemory<char> _6;
    private ReadOnlyMemory<char> _7;
    private ReadOnlyMemory<char> _8;
    private ReadOnlyMemory<char> _9;

    public readonly int MaxLength => _length;
    public int ParsedLength
    {
        get => _parsedLength;
        set => _parsedLength = value;
    }

    public Span<ReadOnlyMemory<char>> MaxLengthSpan()
    {
        return CreateSpan(_length);
    }
    public Span<ReadOnlyMemory<char>> ParsedSpan()
    {
        return CreateSpan(_parsedLength);
    }
    public Span<ReadOnlyMemory<char>> CreateSpan(int length)
    {
        return MemoryMarshal.CreateSpan(ref _0, length);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ParsedExtrasTokens : ITokenStruct
{
    private const int _length = 10;

    private int _parsedLength;

    private ReadOnlyMemory<char> _0;
    private ReadOnlyMemory<char> _1;
    private ReadOnlyMemory<char> _2;
    private ReadOnlyMemory<char> _3;
    private ReadOnlyMemory<char> _4;
    private ReadOnlyMemory<char> _5;
    private ReadOnlyMemory<char> _6;
    private ReadOnlyMemory<char> _7;
    private ReadOnlyMemory<char> _8;
    private ReadOnlyMemory<char> _9;

    public readonly int MaxLength => _length;
    public int ParsedLength
    {
        get => _parsedLength;
        set => _parsedLength = value;
    }

    public Span<ReadOnlyMemory<char>> MaxLengthSpan()
    {
        return CreateSpan(_length);
    }
    public Span<ReadOnlyMemory<char>> ParsedSpan()
    {
        return CreateSpan(_parsedLength);
    }
    public Span<ReadOnlyMemory<char>> CreateSpan(int length)
    {
        return MemoryMarshal.CreateSpan(ref _0, length);
    }
}

internal interface ITokenStruct
{
    public int MaxLength { get; }
    public int ParsedLength { get; set; }

    public Span<ReadOnlyMemory<char>> ParsedSpan();
    public Span<ReadOnlyMemory<char>> MaxLengthSpan();
    public Span<ReadOnlyMemory<char>> CreateSpan(int length);
}
