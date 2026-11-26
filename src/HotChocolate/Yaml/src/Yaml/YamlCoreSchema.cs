using System.Globalization;
using System.Text.RegularExpressions;

namespace HotChocolate.Text.Yaml;

internal static partial class YamlCoreSchema
{
    public static YamlValueKind Resolve(string text)
        => text switch
        {
            "" or "~" or "null" or "Null" or "NULL" => YamlValueKind.Null,
            "true" or "True" or "TRUE" => YamlValueKind.True,
            "false" or "False" or "FALSE" => YamlValueKind.False,
            _ when IntPattern().IsMatch(text) || FloatPattern().IsMatch(text) => YamlValueKind.Number,
            _ => YamlValueKind.String
        };

    public static bool TryParseInt64(string text, out long value)
    {
        if (text.StartsWith("0x", StringComparison.Ordinal))
        {
            var parsed = ulong.TryParse(
                text.AsSpan(2),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var hex);
            value = (long)hex;
            return parsed && hex <= long.MaxValue;
        }

        if (text.StartsWith("0o", StringComparison.Ordinal))
        {
            value = 0;

            foreach (var digit in text.AsSpan(2))
            {
                if (value > (long.MaxValue >> 3))
                {
                    return false;
                }

                value = (value << 3) | (long)(digit - '0');
            }

            return true;
        }

        value = 0;
        return IntPattern().IsMatch(text)
            && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParseDouble(string text, out double value)
    {
        switch (text)
        {
            case ".inf" or ".Inf" or ".INF" or "+.inf" or "+.Inf" or "+.INF":
                value = double.PositiveInfinity;
                return true;

            case "-.inf" or "-.Inf" or "-.INF":
                value = double.NegativeInfinity;
                return true;

            case ".nan" or ".NaN" or ".NAN":
                value = double.NaN;
                return true;
        }

        if (text.StartsWith("0x", StringComparison.Ordinal) || text.StartsWith("0o", StringComparison.Ordinal))
        {
            var parsed = TryParseInt64(text, out var integer);
            value = integer;
            return parsed;
        }

        value = 0;
        return (IntPattern().IsMatch(text) || FloatPattern().IsMatch(text))
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"\A(?:[-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+)\z", RegexOptions.CultureInvariant)]
    private static partial Regex IntPattern();

    [GeneratedRegex(
        @"\A(?:[-+]?(?:\.[0-9]+|[0-9]+(?:\.[0-9]*)?)(?:[eE][-+]?[0-9]+)?|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex FloatPattern();
}
