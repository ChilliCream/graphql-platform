using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Union type extensions are used to represent a union type which has been extended
/// from some original union type. For example, this might be used to represent additional
/// local data, or by a GraphQL service which is itself an extension of another
/// GraphQL service.
/// </summary>
public class UnionTypeExtension : NamedTypeExtensionBase<UnionTypeDefinition>
{
    private Action<IUnionTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new  instance of <see cref="UnionTypeExtension"/>.
    /// </summary>
    public UnionTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="UnionTypeExtension"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public UnionTypeExtension(Action<IUnionTypeDescriptor> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create a union type extension from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The union type definition that specifies the properties of the
    /// newly created union type extension.
    /// </param>
    /// <returns>
    /// Returns the newly created union type extension.
    /// </returns>
    public static UnionTypeExtension CreateUnsafe(UnionTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Union;

    protected override UnionTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = UnionTypeDescriptor.New(context.DescriptorContext);
                _configure!(descriptor);
                return descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        UnionTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        foreach (var typeRef in definition.Types)
        {
            context.Dependencies.Add(new(typeRef));
        }

        TypeDependencyHelper.CollectDirectiveDependencies(definition, context.Dependencies);
    }

    protected override void Merge(
        ITypeCompletionContext context,
        INamedType type)
    {
        if (type is UnionType unionType)
        {
            // we first assert that extension and type are mutable and by
            // this that they do have a type definition.
            AssertMutable();
            unionType.AssertMutable();

            TypeExtensionHelper.MergeContextData(
                Definition!,
                unionType.Definition!);

            TypeExtensionHelper.MergeDirectives(
                context,
                Definition!.Directives,
                unionType.Definition!.Directives);

            TypeExtensionHelper.MergeTypes(
                Definition!.Types,
                unionType.Definition!.Types);

            TypeExtensionHelper.MergeConfigurations(
                Definition!.Configurations,
                unionType.Definition!.Configurations);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.UnionTypeExtension_CannotMerge,
                nameof(type));
        }
    }
}
