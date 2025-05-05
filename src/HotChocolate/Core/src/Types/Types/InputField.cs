using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Serialization.SchemaDebugFormatter;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents an input field of an <see cref="InputObjectType" />.
/// </summary>
public class InputField : FieldBase, IInputValueDefinition, IInputFieldInfo, IHasProperty
{
    private Type _runtimeType = null!;

    /// <summary>
    /// Initializes a new instance of <see cref="InputField"/> with the given
    /// </summary>
    /// <param name="configuration">
    /// The input field configuration.
    /// </param>
    /// <param name="index">
    /// The index of the field.
    /// </param>
    public InputField(InputFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
        DefaultValue = configuration.DefaultValue;
        Property = configuration.Property;

        var formatters = configuration.GetFormatters();
        Formatter = formatters.Count switch
        {
            0 => null,
            1 => formatters[0],
            _ => new AggregateInputValueFormatter(formatters)
        };
    }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new InputObjectType DeclaringType => Unsafe.As<InputObjectType>(base.DeclaringType);

    /// <inheritdoc />
    public override Type RuntimeType => _runtimeType;

    /// <summary>
    /// Gets the default value of this field.
    /// </summary>
    public IValueNode? DefaultValue { get; private set; }

    /// <summary>
    /// Gets the input value formatter.
    /// </summary>
    public IInputValueFormatter? Formatter { get; }

    /// <summary>
    /// Defines if the runtime type is represented as an <see cref="Optional{T}" />.
    /// </summary>
    internal bool IsOptional { get; private set; }

    /// <summary>
    /// Gets the type of the input field.
    /// </summary>
    public new IInputType Type => Unsafe.As<IInputType>(base.Type);

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
    /// Creates a <see cref="InputValueDefinitionNode"/> from this input field.
    /// </summary>
    public InputValueDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ISyntaxNode FormatField()
        => Format(this);
}
