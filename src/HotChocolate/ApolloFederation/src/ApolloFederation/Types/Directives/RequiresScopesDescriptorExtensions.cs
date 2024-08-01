using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Provides extensions for applying @requiresScopes directive on type system descriptors.
/// </summary>
public static class RequiresScopesDescriptorExtensions
{
    /// <summary>
    /// Applies @requiresScopes directive to indicate that the target element is accessible only to the authenticated supergraph users with the appropriate JWT scopes.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @requiresScopes(scopes: [["scope1"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="scopes">Required JWT scopes</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor RequiresScopes(
        this IEnumTypeDescriptor descriptor,
        IReadOnlyList<Scope> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <summary>
    /// Applies @requiresScopes directive to indicate that the target element is accessible only to the authenticated supergraph users with the appropriate JWT scopes.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @requiresScopes(scopes: [["scope1"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="scopes">Required JWT scopes</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor RequiresScopes(
        this IEnumTypeDescriptor descriptor,
        IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes.Select(s => new Scope(s)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{Scope})"/>
    public static IInterfaceFieldDescriptor RequiresScopes(
        this IInterfaceFieldDescriptor descriptor,
        IReadOnlyList<Scope> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IInterfaceFieldDescriptor RequiresScopes(
        this IInterfaceFieldDescriptor descriptor,
        IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes.Select(s => new Scope(s)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{Scope})"/>
    public static IInterfaceTypeDescriptor RequiresScopes(
        this IInterfaceTypeDescriptor descriptor,
        IReadOnlyList<Scope> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IInterfaceTypeDescriptor RequiresScopes(
        this IInterfaceTypeDescriptor descriptor,
        IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes.Select(s => new Scope(s)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{Scope})"/>
    public static IObjectFieldDescriptor RequiresScopes(
        this IObjectFieldDescriptor descriptor,
        IReadOnlyList<Scope> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IObjectFieldDescriptor RequiresScopes(
        this IObjectFieldDescriptor descriptor,
        IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes.Select(s => new Scope(s)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{Scope})"/>
    public static IObjectTypeDescriptor RequiresScopes(
        this IObjectTypeDescriptor descriptor,
        IReadOnlyList<Scope> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IObjectTypeDescriptor RequiresScopes(
        this IObjectTypeDescriptor descriptor,
        IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddScopes(scopes.Select(s => new Scope(s)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    private static void AddScopes(
        IReadOnlyList<Scope> scopes,
        IHasDirectiveDefinition definition,
        ITypeInspector typeInspector)
    {
        var directive = definition
            .Directives
            .Select(t => t.Value)
            .OfType<RequiresScopesDirective>()
            .FirstOrDefault();

        if (directive is null)
        {
            directive = new RequiresScopesDirective([]);
            definition.AddDirective(directive, typeInspector);
        }

        var newScopes = scopes.ToHashSet();
        directive.Scopes.Add(newScopes);
    }
}
