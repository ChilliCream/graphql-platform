using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class EnumValuesMergeBehaviorParser
{
    public const string Auto = "auto";
    public const string Strict = "strict";
    public const string Union = "union";

    public static readonly string[] Values =
    [
        Auto,
        Strict,
        Union
    ];

    public static EnumValuesMergeBehavior? Parse(string value)
        => value switch
        {
            Auto => EnumValuesMergeBehavior.Auto,
            Strict => EnumValuesMergeBehavior.Strict,
            Union => EnumValuesMergeBehavior.Union,
            _ => null
        };

    public static bool TryParse(string value, [NotNullWhen(true)] out EnumValuesMergeBehavior? behavior)
    {
        behavior = Parse(value);
        return behavior is not null;
    }
}
