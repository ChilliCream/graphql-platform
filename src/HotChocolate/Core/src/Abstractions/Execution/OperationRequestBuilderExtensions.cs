using System.Security.Claims;

namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationRequestBuilderExtensions
{
    /// <summary>
    /// Allows introspection usage in the current request.
    /// </summary>
    public static OperationRequestBuilder AllowIntrospection(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(WellKnownContextData.IntrospectionAllowed, null);

    /// <summary>
    /// Sets the error message for when the introspection is not allowed.
    /// </summary>
    public static OperationRequestBuilder SetIntrospectionNotAllowedMessage(
        this OperationRequestBuilder builder,
        string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return builder.SetGlobalState(WellKnownContextData.IntrospectionMessage, message);
    }

    /// <summary>
    /// Sets the error message for when the introspection is not allowed.
    /// </summary>
    public static OperationRequestBuilder SetIntrospectionNotAllowedMessage(
        this OperationRequestBuilder builder,
        Func<string> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);

        return builder.SetGlobalState(WellKnownContextData.IntrospectionMessage, messageFactory);
    }

    /// <summary>
    /// Marks the current request to allow non-persisted operations.
    /// </summary>
    public static OperationRequestBuilder AllowNonPersistedOperation(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(WellKnownContextData.NonPersistedOperationAllowed, true);

    /// <summary>
    /// Skips the request execution depth analysis.
    /// </summary>
    public static OperationRequestBuilder SkipExecutionDepthAnalysis(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(WellKnownContextData.SkipDepthAnalysis, null);

    /// <summary>
    /// Set allowed execution depth for this request and override the
    /// global allowed execution depth.
    /// </summary>
    public static OperationRequestBuilder SetMaximumAllowedExecutionDepth(
        this OperationRequestBuilder builder,
        int maximumAllowedDepth)
        => builder.SetGlobalState(WellKnownContextData.MaxAllowedExecutionDepth, maximumAllowedDepth);

    /// <summary>
    /// Sets the user for this request.
    /// </summary>
    public static OperationRequestBuilder SetUser(
        this OperationRequestBuilder builder,
        ClaimsPrincipal claimsPrincipal)
        => builder.SetGlobalState(nameof(ClaimsPrincipal), claimsPrincipal);
}
