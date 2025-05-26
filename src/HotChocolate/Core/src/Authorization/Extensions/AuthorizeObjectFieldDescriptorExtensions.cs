using HotChocolate.Authorization;

namespace HotChocolate.Types;

/// <summary>
/// Authorize extensions for the object field descriptor.
/// </summary>
public static class AuthorizeObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Adds authorization to a field.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Authorize(
        this IObjectFieldDescriptor descriptor,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (apply is ApplyPolicy.Validation)
        {
            descriptor.ModifyAuthorizationFieldOptions(o => o with { AuthorizeAtRequestLevel = true });
        }

        return descriptor.Directive(new AuthorizeDirective(apply: apply));
    }

    /// <summary>
    /// Adds authorization to a field.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="policy">The authorization policy name.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Authorize(
        this IObjectFieldDescriptor descriptor,
        string policy,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (apply is ApplyPolicy.Validation)
        {
            descriptor.ModifyAuthorizationFieldOptions(o => o with { AuthorizeAtRequestLevel = true });
        }

        return descriptor.Directive(new AuthorizeDirective(policy, apply: apply));
    }

    /// <summary>
    /// Adds authorization to a field.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="roles">The roles for which this field shall be accessible.</param>
    /// <returns>
    /// Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Authorize(
        this IObjectFieldDescriptor descriptor,
        params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(new AuthorizeDirective(roles));
    }

    /// <summary>
    /// Allows anonymous access to this field.
    /// </summary>
    /// <param name="descriptor">
    /// The field descriptor.
    /// </param>
    /// <returns>
    ///  Returns the <see cref="IObjectFieldDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor AllowAnonymous(
        this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Directive(AllowAnonymousDirectiveType.Names.AllowAnonymous);
        descriptor.ModifyAuthorizationFieldOptions(o => o with { AllowAnonymous = true });
        return descriptor;
    }
}
