namespace HotChocolate.CostAnalysis;

/// <summary>
/// The pure maximum response-size arithmetic: object fields count once,
/// lists repeat their contents and alternatives take their maximum.
/// </summary>
internal static class ResponseSizeFieldRule
{
    /// <summary>
    /// Gets the empty response-size summary.
    /// </summary>
    public const double Empty = 0.0;

    /// <summary>
    /// Computes a field's response-size contribution.
    /// </summary>
    public static double Field(double listMultiplier, double child)
        => 1.0 + Scale(listMultiplier, child);

    /// <summary>
    /// Combines fields selected together.
    /// </summary>
    public static double Combine(double left, double right) => left + right;

    /// <summary>
    /// Joins mutually exclusive alternatives.
    /// </summary>
    public static double Join(double left, double right) => double.MaxNumber(left, right);

    private static double Scale(double listMultiplier, double child)
        => child == 0.0 ? 0.0 : listMultiplier * child;
}
