namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Holds the per-request callbacks registered through <c>ModifyCostOptions</c>, applied in
/// registration order to a copy of the gateway's <see cref="FusionCostOptions"/> for that request.
/// </summary>
internal sealed class FusionCostOptionsModifiers
{
    public List<Action<FusionCostOptions>> Modifiers { get; } = [];
}
