using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides descriptor extensions to apply the @requiresScopes directive.
/// </summary>
public static class RequiresScopesDescriptorExtensions
{
    /// <summary>
    /// Adds one @requiresScopes group to the object type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IObjectTypeDescriptor RequiresScopes(
        this IObjectTypeDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    /// <summary>
    /// Adds one @requiresScopes group to the object field. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The object field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IObjectFieldDescriptor RequiresScopes(
        this IObjectFieldDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    /// <summary>
    /// Adds one @requiresScopes group to the interface type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The interface type descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The interface type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IInterfaceTypeDescriptor RequiresScopes(
        this IInterfaceTypeDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    /// <summary>
    /// Adds one @requiresScopes group to the interface field. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The interface field descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The interface field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IInterfaceFieldDescriptor RequiresScopes(
        this IInterfaceFieldDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    /// <summary>
    /// Adds one @requiresScopes group to the enum type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The enum type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IEnumTypeDescriptor RequiresScopes(
        this IEnumTypeDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    /// <summary>
    /// Adds one @requiresScopes group to the scalar type. Calling the method again adds an alternative group.
    /// </summary>
    /// <param name="descriptor">
    /// The scalar type descriptor to apply the directive to.
    /// </param>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    /// <returns>
    /// The scalar type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or <paramref name="scopes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The group is empty or contains a null or empty value.
    /// </exception>
    public static IScalarTypeDescriptor RequiresScopes(
        this IScalarTypeDescriptor descriptor,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(scopes);

        var extension = descriptor.Extend();
        AddGroup(extension.Configuration, extension.Context.TypeInspector, scopes);
        return descriptor;
    }

    private static void AddGroup(
        IDirectiveConfigurationProvider configuration,
        ITypeInspector typeInspector,
        string[] scopes)
    {
        var groups = new List<IReadOnlyList<string>>();

        for (var i = configuration.Directives.Count - 1; i >= 0; i--)
        {
            if (configuration.Directives[i].Value is RequiresScopes existing)
            {
                groups.AddRange(existing.Scopes);
            }
        }

        groups.Add(scopes);
        var requiresScopes = new RequiresScopes(groups);

        for (var i = configuration.Directives.Count - 1; i >= 0; i--)
        {
            if (configuration.Directives[i].Value is RequiresScopes)
            {
                configuration.Directives.RemoveAt(i);
            }
        }

        configuration.AddDirective(requiresScopes, typeInspector);
    }
}
