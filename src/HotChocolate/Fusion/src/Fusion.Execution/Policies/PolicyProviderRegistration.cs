namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Records whether a schema generation's service collection had a user-registered
/// <see cref="IPolicyProvider"/> before the gateway decorated it with a <see cref="CompositePolicyProvider"/>.
/// </summary>
/// <remarks>
/// <c>CompositeSchemaBuilder</c> needs this fact before it knows whether the schema references
/// any built-in policy name, so it can keep returning an empty <see cref="PolicyCollection"/> for
/// a schema with neither, without resolving (and so constructing) the composite just to check.
/// </remarks>
internal sealed class PolicyProviderRegistration
{
    public required bool HasUserProvider { get; init; }
}
