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

    public InputField(InputFieldConfiguration definition, int index)
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
        FieldConfiguration definition)
        => OnCompleteField(context, declaringMember, (InputFieldConfiguration)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        Type = context.GetType<IInputType>(definition.Type!).EnsureInputType();
        _runtimeType = definition.RuntimeType ?? definition.Property?.PropertyType!;
        _runtimeType = CompleteRuntimeType(Type, _runtimeType, out var isOptional);
        IsOptional = isOptional;
    }

    protected sealed override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnCompleteMetadata(context, declaringMember, (InputFieldConfiguration)definition);

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
    {
        base.OnCompleteMetadata(context, declaringMember, definition);
        DefaultValue = CompleteDefaultValue(context, definition, Type, Coordinate);
    }

    protected sealed override void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnMakeExecutable(context, declaringMember, (InputFieldConfiguration)definition);

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
        => base.OnMakeExecutable(context, declaringMember, definition);

    protected sealed override void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => base.OnFinalizeField(context, declaringMember, (InputFieldConfiguration)definition);

    protected virtual void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
        => base.OnFinalizeField(context, declaringMember, definition);



    /// <summary>
    /// Returns a string that represents the current field.
    /// </summary>
    /// <returns>
    /// A string that represents the current field.
    /// </returns>
    public override string ToString() => $"{Name}:{Type.Print()}";
}
