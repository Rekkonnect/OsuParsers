using OsuParsers.Decoders;
using System;

namespace OsuParsers.Helpers;

public static class HighPerformanceExtensions
{
    internal static void SplitToTokenStruct<TTokenStruct>(
        this string s,
        char delimiter,
        scoped ref TTokenStruct tokenStruct)
        where TTokenStruct : struct, ITokenStruct
    {
        SplitToTokenStruct(s.AsMemory(), delimiter, ref tokenStruct);
    }

    internal static void SplitToTokenStruct<TTokenStruct>(
        this ReadOnlyMemory<char> s,
        char delimiter,
        scoped ref TTokenStruct tokenStruct)
        where TTokenStruct : struct, ITokenStruct
    {
        var splitSegments = tokenStruct.MaxLengthSpan();
        SplitToMemory(s, delimiter, splitSegments, out int splitCount);
        tokenStruct.ParsedLength = splitCount;
    }

    public static void SplitToMemoryString(
        this string s,
        char delimiter,
        scoped Span<ReadOnlyMemory<char>> splitSegments)
    {
        SplitToMemory(s.AsMemory(), delimiter, splitSegments, out _);
    }

    public static void SplitToMemory(
        this ReadOnlyMemory<char> s,
        char delimiter,
        scoped Span<ReadOnlyMemory<char>> splitSegments,
        out int splitCount)
    {
        var span = s.Span;
        int startIndex = 0;
        splitCount = splitSegments.Length;
        for (int i = 0; i < splitSegments.Length; i++)
        {
            int segmentLength = span.IndexOf(delimiter);

            if (segmentLength < 0)
            {
                // terminate
                var trailingSegment = s[startIndex..];
                splitSegments[i] = trailingSegment;
                splitCount = i + 1;
                break;
            }

            // advance span
            int nextSegmentStart = segmentLength + 1;
            span = span[nextSegmentStart..];
            var segment = s.Slice(startIndex, segmentLength);
            startIndex += nextSegmentStart;
            splitSegments[i] = segment;
        }
    }

    public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> source, char value)
    {
        return source
            .TrimStart(value)
            .TrimEnd(value);
    }

    public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> source, char value)
    {
        int length = source.Length;
        int index = 0;
        while (index < length)
        {
            if (source[index] != value)
            {
                break;
            }

            index++;
        }
        return source[index..];
    }

    public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> source, char value)
    {
        int length = source.Length;
        while (length >= 0)
        {
            if (source[length - 1] != value)
            {
                break;
            }

            length--;
        }
        return source[..length];
    }

    public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> source, char value)
    {
        return source
            .TrimStart(value)
            .TrimEnd(value);
    }

    public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> source, char value)
    {
        int length = source.Length;
        int index = 0;
        var span = source.Span;
        while (index < length)
        {
            if (span[index] != value)
            {
                break;
            }

            index++;
        }
        return source[index..];
    }

    public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> source, char value)
    {
        int length = source.Length;
        var span = source.Span;
        while (length >= 0)
        {
            if (span[length - 1] != value)
            {
                break;
            }

            length--;
        }
        return source[..length];
    }

    public static bool SplitOnce(
        this ReadOnlyMemory<char> source,
        char delimiter,
        out ReadOnlyMemory<char> left,
        out ReadOnlyMemory<char> right)
    {
        var span = source.Span;
        int index = span.IndexOf(delimiter);

        if (index < 0)
        {
            left = source;
            right = default;
            return false;
        }

        int rightStart = index + 1;
        left = source[..index];
        right = source[rightStart..];
        return true;
    }

    public static T Last<T>(this Span<T> source)
    {
        return source[^1];
    }

    public static bool Contains(this ReadOnlySpan<char> source, char value)
    {
        return source.IndexOf(value) >= 0;
    }
}
