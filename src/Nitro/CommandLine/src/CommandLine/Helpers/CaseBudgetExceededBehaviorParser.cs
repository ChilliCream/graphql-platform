using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class CaseBudgetExceededBehaviorParser
{
    public const string EvaluatePerRequest = "evaluate-per-request";
    public const string Overestimate = "overestimate";

    public static readonly string[] Values =
    [
        EvaluatePerRequest,
        Overestimate
    ];

    public static CaseBudgetExceededBehavior? Parse(string value)
        => value switch
        {
            EvaluatePerRequest => CaseBudgetExceededBehavior.EvaluatePerRequest,
            Overestimate => CaseBudgetExceededBehavior.Overestimate,
            _ => null
        };

    public static bool TryParse(
        string value,
        [NotNullWhen(true)] out CaseBudgetExceededBehavior? behavior)
    {
        behavior = Parse(value);
        return behavior is not null;
    }
}
