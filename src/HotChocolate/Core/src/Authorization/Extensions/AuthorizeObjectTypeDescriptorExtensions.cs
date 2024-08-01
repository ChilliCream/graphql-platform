using HotChocolate.Authorization;

namespace HotChocolate.Types;

/// <summary>
/// Authorize extensions for the object type descriptor.
/// </summary>
public static class AuthorizeObjectTypeDescriptorExtensions
{
    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Authorize(
        this IObjectTypeDescriptor descriptor,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(apply: apply));
    }

    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="policy">The authorization policy name.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Authorize(
        this IObjectTypeDescriptor descriptor,
        string policy,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(policy, apply: apply));
    }

    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="roles">The roles for which this field shall be accessible.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Authorize(
        this IObjectTypeDescriptor descriptor,
        params string[] roles)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(roles));
    }

    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> Authorize<T>(
        this IObjectTypeDescriptor<T> descriptor,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(apply: apply));
    }

    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="policy">The authorization policy name.</param>
    /// <param name="apply">Defines when the authorization policy is invoked.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> Authorize<T>(
        this IObjectTypeDescriptor<T> descriptor,
        string policy,
        ApplyPolicy apply = ApplyPolicy.BeforeResolver)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(policy, apply: apply));
    }

    /// <summary>
    /// Adds authorization to a type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    /// <param name="roles">The roles for which this field shall be accessible.</param>
    /// <returns>
    /// Returns the <see cref="IObjectTypeDescriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> Authorize<T>(
        this IObjectTypeDescriptor<T> descriptor,
        params string[] roles)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new AuthorizeDirective(roles));
    }
}
