using HotChocolate.Authorization;
using static HotChocolate.WellKnownContextData;

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
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (apply is ApplyPolicy.Validation)
        {
            descriptor.Extend().Context.ContextData[AuthorizationRequestPolicy] = true;
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
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (apply is ApplyPolicy.Validation)
        {
            descriptor.Extend().Context.ContextData[AuthorizationRequestPolicy] = true;
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
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

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
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Directive(AllowAnonymousDirectiveType.Names.AllowAnonymous);
        descriptor.Extend().Definition.ContextData[WellKnownContextData.AllowAnonymous] = true;
        return descriptor;
    }
}
