using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class DirectiveMergeBehaviorParser
{
    public const string Ignore = "ignore";
    public const string Include = "include";
    public const string IncludePrivate = "include-private";

    public static readonly string[] Values =
    [
        Ignore,
        Include,
        IncludePrivate
    ];

    public static DirectiveMergeBehavior? Parse(string value)
        => value switch
        {
            Ignore => DirectiveMergeBehavior.Ignore,
            Include => DirectiveMergeBehavior.Include,
            IncludePrivate => DirectiveMergeBehavior.IncludePrivate,
            _ => null
        };

    public static bool TryParse(
        string value,
        [NotNullWhen(true)] out DirectiveMergeBehavior? directiveMergeBehavior)
    {
        directiveMergeBehavior = Parse(value);
        return directiveMergeBehavior is not null;
    }
}
