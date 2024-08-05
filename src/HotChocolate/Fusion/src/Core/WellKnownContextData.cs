namespace HotChocolate.Fusion;

/// <summary>
/// well-known context data keys for the fusion execution.
/// </summary>
internal static class FusionContextDataKeys
{
    /// <summary>
    /// The response state key that is used to store the query plan.
    /// </summary>
    public const string QueryPlanProp = "queryPlan";

    /// <summary>
    /// The response state key that is used to store the query plan hash.
    /// </summary>
    public const string QueryPlanHashProp = "queryPlanHash";
}
