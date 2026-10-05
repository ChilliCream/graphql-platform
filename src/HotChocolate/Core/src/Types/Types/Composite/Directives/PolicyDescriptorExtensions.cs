namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides descriptor extensions to apply the @policy directive.
/// </summary>
public static class PolicyDescriptorExtensions
{
    /// <summary>
    /// Adds one @policy group to the object type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }

    /// <summary>
    /// Adds one @policy group to the object field. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The object field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }

    /// <summary>
    /// Adds one @policy group to the interface type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The interface type descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The interface type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }

    /// <summary>
    /// Adds one @policy group to the interface field. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The interface field descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The interface field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }

    /// <summary>
    /// Adds one @policy group to the enum type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The enum type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IEnumTypeDescriptor Policy(
        this IEnumTypeDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }

    /// <summary>
    /// Adds one @policy group to the scalar type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The scalar type descriptor to apply the directive to.
    /// </param>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    /// <returns>
    /// The scalar type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="policies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IScalarTypeDescriptor Policy(
        this IScalarTypeDescriptor descriptor,
        params string[] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(policies);

        var extension = descriptor.Extend();
        AuthorizationGroups.AddGroup<Policy>(
            extension.Configuration,
            extension.Context.TypeInspector,
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies),
            static directive => directive.Policies,
            static groups => new Policy(groups));
        return descriptor;
    }
}
