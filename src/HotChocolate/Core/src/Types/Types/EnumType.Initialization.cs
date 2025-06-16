using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types;

public partial class EnumType
{
    private FrozenDictionary<object, EnumValue> _valueLookup = null!;
    private EnumValueCollection _values = null!;
    private INamingConventions _naming = null!;
    private Action<IEnumTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new instance of <see cref="EnumType"/>.
    /// </summary>
    protected EnumType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate defining the configuration.
    /// </param>
    public EnumType(Action<IEnumTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create an enum type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The enum type definition that specifies the properties of the newly created enum type.
    /// </param>
    /// <returns>
    /// Returns the newly created enum type.
    /// </returns>
    public static EnumType CreateUnsafe(EnumTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <summary>
    /// Override this in order to specify the type configuration explicitly.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor of this type lets you express the type configuration.
    /// </param>
    protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

    /// <inheritdoc />
    protected override EnumTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = EnumTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
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

    /// <inheritdoc />
    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        EnumTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);
        context.RegisterDependencies(configuration);
        SetTypeIdentity(typeof(EnumType<>));
    }

    /// <inheritdoc />
    protected override void OnCompleteType(
        ITypeCompletionContext context,
        EnumTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        var builder = new List<EnumValue>(configuration.Values.Count);
        var valueLookupBuilder = new Dictionary<object, EnumValue>(configuration.ValueComparer);
        _naming = context.DescriptorContext.Naming;

        foreach (var enumValueDefinition in configuration.Values)
        {
            if (enumValueDefinition.Ignore)
            {
                continue;
            }

            if (TryCreateEnumValue(context, enumValueDefinition, out var enumValue))
            {
                valueLookupBuilder[enumValue.Value] = enumValue;
                builder.Add(enumValue);
            }
        }

        if (builder.Count == 0)
        {
            context.ReportError(
                SchemaErrorBuilder.New()
                    .SetMessage(TypeResources.EnumType_NoValues, Name)
                    .SetCode(ErrorCodes.Schema.NoEnumValues)
                    .SetTypeSystemObject(this)
                    .Build());
        }

        _values = new EnumValueCollection([.. builder], configuration.NameComparer);
        _valueLookup = valueLookupBuilder.ToFrozenDictionary(configuration.ValueComparer);
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        EnumTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        foreach (var value in _values.OfType<IEnumValueCompletion>())
        {
            value.CompleteMetadata(context, this);
        }
    }

    protected virtual bool TryCreateEnumValue(
        ITypeCompletionContext context,
        EnumValueConfiguration definition,
        [NotNullWhen(true)] out EnumValue? enumValue)
    {
        enumValue = new DefaultEnumValue(definition);
        return true;
    }
}
