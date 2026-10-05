namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides descriptor extensions to apply the @authenticated directive.
/// </summary>
public static class AuthenticatedDescriptorExtensions
{
    /// <summary>
    /// Applies the @authenticated directive to the object type.
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Authenticated(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }

    /// <summary>
    /// Applies the @authenticated directive to the object field.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The object field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Authenticated(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }

    /// <summary>
    /// Applies the @authenticated directive to the interface type.
    /// </summary>
    /// <param name="descriptor">
    /// The interface type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The interface type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInterfaceTypeDescriptor Authenticated(this IInterfaceTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }

    /// <summary>
    /// Applies the @authenticated directive to the interface field.
    /// </summary>
    /// <param name="descriptor">
    /// The interface field descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The interface field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInterfaceFieldDescriptor Authenticated(this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }

    /// <summary>
    /// Applies the @authenticated directive to the enum type.
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The enum type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Authenticated(this IEnumTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }

    /// <summary>
    /// Applies the @authenticated directive to the scalar type.
    /// </summary>
    /// <param name="descriptor">
    /// The scalar type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The scalar type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IScalarTypeDescriptor Authenticated(this IScalarTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Authenticated.Instance);
    }
}
