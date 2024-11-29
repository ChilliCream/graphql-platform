using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Enum type extensions are used to represent an enum type which has been extended from
/// some original enum type. For example, this might be used to represent additional local data,
/// or by a GraphQL service which is itself an extension of another GraphQL service.
/// </summary>
public class EnumTypeExtension : NamedTypeExtensionBase<EnumTypeDefinition>
{
    private Action<IEnumTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeExtension"/>.
    /// </summary>
    protected EnumTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeExtension"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate defining the configuration.
    /// </param>
    public EnumTypeExtension(Action<IEnumTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create an enum type extension from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The enum type definition that specifies the properties of
    /// the newly created enum type extension.
    /// </param>
    /// <returns>
    /// Returns the newly created enum type extension.
    /// </returns>
    public static EnumTypeExtension CreateUnsafe(EnumTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Enum;

    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = EnumTypeDescriptor.New(context.DescriptorContext);
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

    /// <summary>
    /// Override this in order to specify the type configuration explicitly.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor of this type lets you express the type configuration.
    /// </param>
    protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        EnumTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
    }

    protected override void Merge(
        ITypeCompletionContext context,
        INamedType type)
    {
        if (type is EnumType enumType)
        {
            // we first assert that extension and type are mutable and by
            // this that they do have a type definition.
            AssertMutable();
            enumType.AssertMutable();

            TypeExtensionHelper.MergeContextData(
                Definition!,
                enumType.Definition!);

            TypeExtensionHelper.MergeDirectives(
                context,
                Definition!.Directives,
                enumType.Definition!.Directives);

            TypeExtensionHelper.MergeConfigurations(
                Definition!.Configurations,
                enumType.Definition!.Configurations);

            MergeValues(context, Definition!, enumType.Definition!);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.EnumTypeExtension_CannotMerge,
                nameof(type));
        }
    }

    private void MergeValues(
        ITypeCompletionContext context,
        EnumTypeDefinition extension,
        EnumTypeDefinition type)
    {
        foreach (var enumValue in
            extension.Values.Where(t => t.RuntimeValue != null))
        {
            if (type.RuntimeType.IsInstanceOfType(enumValue.RuntimeValue))
            {
                var existingValue = type.Values.FirstOrDefault(
                    t => Equals(enumValue.RuntimeValue, t.RuntimeValue));

                if (existingValue is null)
                {
                    type.Values.Add(enumValue);
                }
                else
                {
                    existingValue.Ignore = enumValue.Ignore;

                    TypeExtensionHelper.MergeContextData(enumValue, existingValue);

                    TypeExtensionHelper.MergeDirectives(
                        context,
                        enumValue.Directives,
                        existingValue.Directives);
                }
            }
            else
            {
                context.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.EnumTypeExtension_ValueTypeInvalid,
                            enumValue.RuntimeValue))
                        .SetTypeSystemObject(this)
                        .Build());
            }
        }
    }
}
