using System.Diagnostics.CodeAnalysis;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime;

internal static class PatternExtensions
{
    public static bool TryParse<NodaTimeType>(
        this IPattern<NodaTimeType> pattern,
        string text,
        [NotNullWhen(true)] out NodaTimeType? output)
        where NodaTimeType : struct
    {
        var result = pattern.Parse(text);

        if (result.Success)
        {
            output = result.Value;
            return true;
        }

        output = null;
        return false;
    }

    public static bool TryParse<NodaTimeType>(
        this IPattern<NodaTimeType> pattern,
        string text,
        [NotNullWhen(true)] out NodaTimeType? output)
        where NodaTimeType : class
    {
        ParseResult<NodaTimeType> result = pattern.Parse(text);

        if (result.Success)
        {
            output = result.Value;
            return true;
        }

        output = null;
        return false;
    }

    public static bool TryParse<NodaTimeType>(
        this IPattern<NodaTimeType>[] patterns,
        string text,
        [NotNullWhen(true)] out NodaTimeType? output)
        where NodaTimeType : struct
    {
        foreach (var pattern in patterns)
        {
            if (pattern.TryParse(text, out output))
            {
                return true;
            }
        }

        output = null;
        return false;
    }

    public static bool TryParse<NodaTimeType>(
        this IPattern<NodaTimeType>[] patterns,
        string text,
        [NotNullWhen(true)] out NodaTimeType? output)
        where NodaTimeType : class
    {
        foreach (IPattern<NodaTimeType> pattern in patterns)
        {
            if (pattern.TryParse(text, out output))
            {
                return true;
            }
        }

        output = null;
        return false;
    }
}
