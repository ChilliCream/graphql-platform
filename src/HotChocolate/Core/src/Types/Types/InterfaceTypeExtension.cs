using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// Interface type extensions are used to represent an interface which has been extended
/// from some original interface.
/// </para>
/// <para>
/// For example, this might be used to represent common local data on many types,
/// or by a GraphQL service which is itself an extension of another GraphQL service.
/// </para>
/// </summary>
public class InterfaceTypeExtension : NamedTypeExtensionBase<InterfaceTypeConfiguration>
{
    private Action<IInterfaceTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceTypeExtension"/>.
    /// </summary>
    protected InterfaceTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceTypeExtension"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public InterfaceTypeExtension(Action<IInterfaceTypeDescriptor> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Create an interface type extension from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The interface type definition that specifies the properties of the
    /// newly created interface type extension.
    /// </param>
    /// <returns>
    /// Returns the newly created interface type extension.
    /// </returns>
    public static InterfaceTypeExtension CreateUnsafe(InterfaceTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Interface;

    protected override InterfaceTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = InterfaceTypeDescriptor.New(context.DescriptorContext);
                _configure!(descriptor);
                return descriptor.CreateConfiguration();
            }

            return Configuration;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InterfaceTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        context.RegisterDependencies(configuration);
    }

    protected override void Merge(
        ITypeCompletionContext context,
        ITypeDefinition type)
    {
        if (type is InterfaceType interfaceType)
        {
            // we first assert that extension and type are mutable and by
            // this that they do have a type definition.
            AssertMutable();
            interfaceType.AssertMutable();

            TypeExtensionHelper.MergeFeatures(
                Configuration!,
                interfaceType.Configuration!);

            TypeExtensionHelper.MergeDirectives(
                context,
                Configuration!.Directives,
                interfaceType.Configuration!.Directives);

            TypeExtensionHelper.MergeInterfaceFields(
                context,
                Configuration!.Fields,
                interfaceType.Configuration!.Fields);

            TypeExtensionHelper.MergeConfigurations(
                Configuration!.Tasks,
                interfaceType.Configuration!.Tasks);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.InterfaceTypeExtension_CannotMerge,
                nameof(type));
        }
    }
}
