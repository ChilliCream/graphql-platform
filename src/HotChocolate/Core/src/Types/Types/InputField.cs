using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types;

public class InputField : FieldBase, IInputField, IHasProperty
{
    private Type _runtimeType = default!;

    public InputField(InputFieldDefinition definition, int index)
        : base(definition, index)
    {
        DefaultValue = definition.DefaultValue;
        Property = definition.Property;

        var formatters = definition.GetFormatters();
        Formatter = formatters.Count switch
        {
            0 => null,
            1 => formatters[0],
            _ => new AggregateInputValueFormatter(formatters),
        };

        IsDeprecated = !string.IsNullOrEmpty(definition.DeprecationReason);
        DeprecationReason = definition.DeprecationReason;
    }

    /// <inheritdoc />
    public IInputType Type { get; private set; } = default!;

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new InputObjectType DeclaringType => (InputObjectType)base.DeclaringType;

    /// <inheritdoc />
    public override Type RuntimeType => _runtimeType;

    /// <inheritdoc />
    public IValueNode? DefaultValue { get; private set; }

    /// <inheritdoc />
    public IInputValueFormatter? Formatter { get; }

    /// <summary>
    /// Defines if the runtime type is represented as an <see cref="Optional{T}" />.
    /// </summary>
    internal bool IsOptional { get; private set; }

    /// <inheritdoc />
    public bool IsDeprecated { get; }

    /// <inheritdoc />
    public string? DeprecationReason { get; }

    /// <summary>
    /// If this field is bound to a property on a concrete model,
    /// then this property exposes this property.
    /// </summary>
    public PropertyInfo? Property { get; }

    protected sealed override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnCompleteField(context, declaringMember, (InputFieldDefinition)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldDefinition definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        Type = context.GetType<IInputType>(definition.Type!).EnsureInputType();
        _runtimeType = definition.RuntimeType ?? definition.Property?.PropertyType!;
        _runtimeType = CompleteRuntimeType(Type, _runtimeType, out var isOptional);
        DefaultValue = CompleteDefaultValue(context, definition, Type, Coordinate);
        IsOptional = isOptional;
    }

    /// <summary>
    /// Returns a string that represents the current field.
    /// </summary>
    /// <returns>
    /// A string that represents the current field.
    /// </returns>
    public override string ToString() => $"{Name}:{Type.Print()}";
}
