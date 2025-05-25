namespace HotChocolate.Execution;

/// <summary>
/// Represents GraphQL request options overrides for the introspection rule.
/// </summary>
/// <param name="IsAllowed">
/// A value indicating whether introspection is allowed.
/// </param>
/// <param name="NotAllowedErrorMessage">
/// A custom error message that is being used when introspection is not allowed.
/// </param>
public sealed record IntrospectionRequestOverrides(
    bool IsAllowed = true,
    string? NotAllowedErrorMessage = null);
