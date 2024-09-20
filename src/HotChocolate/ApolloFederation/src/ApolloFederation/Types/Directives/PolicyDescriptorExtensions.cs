using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Provides extensions for applying @policy directive on type system descriptors.
/// </summary>
public static class PolicyDescriptorExtensions
{
    /// <summary>
    /// Applies @policy directive to indicate that the target element is restricted based on authorization policies that are evaluated in a Rhai script or coprocessor.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @policy(policies: [["policy1"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="policies">Required authorization policies</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Policy(
        this IEnumTypeDescriptor descriptor,
        IReadOnlyList<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <summary>
    /// Applies @policy directive to indicate that the target element is restricted based on
    /// authorization policies that are evaluated in a Rhai script or coprocessor.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @policy(policies: [["policy1"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="policies">Required authorization policies</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Policy(
        this IEnumTypeDescriptor descriptor,
        IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies.Select(p => new Policy(p)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{HotChocolate.ApolloFederation.Types.Policy})"/>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        IReadOnlyList<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies.Select(p => new Policy(p)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{HotChocolate.ApolloFederation.Types.Policy})"/>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        IReadOnlyList<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies.Select(p => new Policy(p)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{HotChocolate.ApolloFederation.Types.Policy})"/>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        IReadOnlyList<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies.Select(p => new Policy(p)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{HotChocolate.ApolloFederation.Types.Policy})"/>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        IReadOnlyList<Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies, def, ctx.TypeInspector);
            });

        return descriptor;
    }

    /// <inheritdoc cref="PolicyDescriptorExtensions.Policy(IEnumTypeDescriptor, IReadOnlyList{string})"/>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Extend().OnBeforeCreate(
            (ctx, def) =>
            {
                AddPolicies(policies.Select(p => new Policy(p)).ToArray(), def, ctx.TypeInspector);
            });

        return descriptor;
    }

    private static void AddPolicies(
        IReadOnlyList<Policy> policies,
        IHasDirectiveDefinition definition,
        ITypeInspector typeInspector)
    {
        var directive = definition
            .Directives
            .Select(t => t.Value)
            .OfType<PolicyDirective>()
            .FirstOrDefault();

        if (directive is null)
        {
            directive = new PolicyDirective([]);
            definition.AddDirective(directive, typeInspector);
        }

        var newPolicies = policies.ToHashSet();
        directive.Policies.Add(newPolicies);
    }
}
