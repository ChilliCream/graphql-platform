using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Input object type extensions are used to represent an input object type
/// which has been extended from some original input object type. For example,
/// this might be used by a GraphQL service which is itself an extension of another
/// GraphQL service.
/// </summary>
public class InputObjectTypeExtension : NamedTypeExtensionBase<InputObjectTypeDefinition>
{
    private Action<IInputObjectTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new  instance of <see cref="InputObjectTypeExtension"/>.
    /// </summary>
    protected InputObjectTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="InputObjectTypeExtension"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public InputObjectTypeExtension(Action<IInputObjectTypeDescriptor> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Create an input object type extension from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The input object type definition that specifies the properties of the
    /// newly created input object type extension.
    /// </param>
    /// <returns>
    /// Returns the newly created input object type.
    /// </returns>
    public static InputObjectTypeExtension CreateUnsafe(InputObjectTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.InputObject;

    protected override InputObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = InputObjectTypeDescriptor.New(
                    context.DescriptorContext);
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

    protected virtual void Configure(IInputObjectTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
    }

    protected override void Merge(
        ITypeCompletionContext context,
        INamedType type)
    {
        if (type is InputObjectType inputObjectType)
        {
            // we first assert that extension and type are mutable and by
            // this that they do have a type definition.
            AssertMutable();
            inputObjectType.AssertMutable();

            TypeExtensionHelper.MergeContextData(
                Definition!,
                inputObjectType.Definition!);

            TypeExtensionHelper.MergeDirectives(
                context,
                Definition!.Directives!,
                inputObjectType.Definition!.Directives);

            TypeExtensionHelper.MergeInputObjectFields(
                context,
                Definition!.Fields,
                inputObjectType.Definition!.Fields);

            TypeExtensionHelper.MergeConfigurations(
                Definition!.Configurations,
                inputObjectType.Definition!.Configurations);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.InputObjectTypeExtension_CannotMerge,
                nameof(type));
        }
    }
}
