using HotChocolate.Features;

namespace HotChocolate.Execution;

public static class IntrospectionRequestOverridesExtensions
{
    /// <summary>
    /// Allows introspection usage for the current request.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder AllowIntrospection(
        this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<IntrospectionRequestOverrides>();

        if (options is null)
        {
            options = new IntrospectionRequestOverrides(IsAllowed: true);
        }
        else
        {
            options = options with { IsAllowed = true };
        }

        builder.Features.Set(options);
        return builder;
    }

    /// <summary>
    /// Sets the error message for when the introspection is not allowed.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="message">
    /// The error message that is being used when introspection is not allowed.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder SetIntrospectionNotAllowedMessage(
        this OperationRequestBuilder builder,
        string message)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<IntrospectionRequestOverrides>();

        if (options is null)
        {
            options = new IntrospectionRequestOverrides(
                IsAllowed: false,
                NotAllowedErrorMessage: message);
        }
        else
        {
            options = options with { NotAllowedErrorMessage = message };
        }

        builder.Features.Set(options);
        return builder;
    }

    /// <summary>
    /// Checks if introspection is disabled for the current request.
    /// </summary>
    /// <param name="featureProvider">
    /// The feature provider.
    /// </param>
    /// <returns>
    /// Returns true if introspection is disabled, otherwise false.
    /// </returns>
    public static bool IsIntrospectionDisabled(this IFeatureProvider featureProvider)
        => featureProvider.Features.Get<IntrospectionRequestOverrides>()?.IsAllowed != true;

    /// <summary>
    /// Gets the custom error message for when introspection is not allowed.
    /// </summary>
    /// <param name="featureProvider">
    /// The feature provider.
    /// </param>
    /// <returns>
    /// Returns the custom error message for when introspection is not allowed.
    /// </returns>
    public static string? GetCustomIntrospectionErrorMessage(this IFeatureProvider featureProvider)
        => featureProvider.Features.Get<IntrospectionRequestOverrides>()?.NotAllowedErrorMessage;
}
