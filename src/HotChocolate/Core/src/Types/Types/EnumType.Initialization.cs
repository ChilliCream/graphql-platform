using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public partial class EnumType
{
    private FrozenDictionary<string, IEnumValue> _nameLookup = default!;
    private FrozenDictionary<object, IEnumValue> _valueLookup = default!;
    private ImmutableArray<IEnumValue> _values;
    private INamingConventions _naming = default!;
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
    public static EnumType CreateUnsafe(EnumTypeDefinition definition)
        => new() { Definition = definition, };

    /// <summary>
    /// Override this in order to specify the type configuration explicitly.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor of this type lets you express the type configuration.
    /// </param>
    protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

    /// <inheritdoc />
    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = EnumTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
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

    /// <inheritdoc />
    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        EnumTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
        SetTypeIdentity(typeof(EnumType<>));
    }

    /// <inheritdoc />
    protected override void OnCompleteType(
        ITypeCompletionContext context,
        EnumTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        var builder = ImmutableArray.CreateBuilder<IEnumValue>(definition.Values.Count);
        var nameLookupBuilder = new Dictionary<string, IEnumValue>(definition.NameComparer);
        var valueLookupBuilder = new Dictionary<object, IEnumValue>(definition.ValueComparer);
        _naming = context.DescriptorContext.Naming;

        foreach (var enumValueDefinition in definition.Values)
        {
            if (enumValueDefinition.Ignore)
            {
                continue;
            }

            if (TryCreateEnumValue(context, enumValueDefinition, out var enumValue))
            {
                nameLookupBuilder[enumValue.Name] = enumValue;
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

        _values = builder.ToImmutable();
        _nameLookup = nameLookupBuilder.ToFrozenDictionary(definition.NameComparer);
        _valueLookup = valueLookupBuilder.ToFrozenDictionary(definition.ValueComparer);
    }

    protected virtual bool TryCreateEnumValue(
        ITypeCompletionContext context,
        EnumValueDefinition definition,
        [NotNullWhen(true)] out IEnumValue? enumValue)
    {
        enumValue = new EnumValue(context, definition);
        return true;
    }
}
