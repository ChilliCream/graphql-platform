using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

/// <summary>
/// Represents a field or directive argument.
/// </summary>
public class Argument : FieldBase, IInputValueDefinition, IInputValueInfo
{
    private Type _runtimeType = null!;

    /// <summary>
    /// Initializes a new <see cref="Argument"/>.
    /// </summary>
    /// <param name="definition">
    /// The argument definition.
    /// </param>
    /// <param name="index">
    /// The position of this argument within its field collection.
    /// </param>
    public Argument(ArgumentConfiguration definition, int index)
        : base(definition, index)
    {
        DefaultValue = definition.DefaultValue;

        var formatters = definition.GetFormatters();

        if (formatters.Count == 0)
        {
            Formatter = null;
        }
        else if (formatters.Count == 1)
        {
            Formatter = formatters[0];
        }
        else
        {
            Formatter = new AggregateInputValueFormatter(formatters);
        }
    }

    /// <summary>
    /// Gets the type that declares the field to which this argument belongs to.
    /// </summary>
    public new IOutputTypeDefinition DeclaringType => Unsafe.As<IOutputTypeDefinition>(base.DeclaringType);

    /// <summary>
    /// Gets the type of this field.
    /// </summary>
    public new IInputType Type => Unsafe.As<IInputType>(base.Type);

    /// <inheritdoc />
    public override Type RuntimeType => _runtimeType;

    /// <inheritdoc cref="IInputValueDefinition.DefaultValue" />
    public IValueNode? DefaultValue { get; private set; }

    /// <inheritdoc />
    public IInputValueFormatter? Formatter { get; }

    /// <summary>
    /// Defines if the runtime type is represented as an <see cref="Optional{T}" />.
    /// </summary>
    internal bool IsOptional { get; private set; }

    protected sealed override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnCompleteField(context, declaringMember, (ArgumentConfiguration)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ArgumentConfiguration definition)
    {
        if (definition.Type is null)
        {
            context.ReportError(SchemaErrorBuilder.New()
                .SetMessage(TypeResources.Argument_TypeIsNull, definition.Name)
                .SetTypeSystemObject(context.Type)
                .SetExtension("declaringMember", declaringMember)
                .SetExtension("name", definition.Name)
                .Build());
            return;
        }

        base.OnCompleteField(context, declaringMember, definition);

        _runtimeType = definition.GetRuntimeType()!;
        _runtimeType = CompleteRuntimeType(Type, _runtimeType, out var isOptional);
        IsOptional = isOptional;
    }

    protected sealed override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnCompleteMetadata(context, declaringMember, (ArgumentConfiguration)definition);

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ArgumentConfiguration definition)
    {
        DefaultValue = CompleteDefaultValue(context, definition, Type, Coordinate);
        base.OnCompleteMetadata(context, declaringMember, definition);
    }

    protected sealed override void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnMakeExecutable(context, declaringMember, (ArgumentConfiguration)definition);

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ArgumentConfiguration definition) =>
        base.OnMakeExecutable(context, declaringMember, definition);

    protected sealed override void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnFinalizeField(context, declaringMember, (ArgumentConfiguration)definition);

    protected virtual void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ArgumentConfiguration definition) =>
        base.OnFinalizeField(context, declaringMember, definition);

    public InputValueDefinitionNode ToSyntaxNode() => Format(this);

    protected override ISyntaxNode FormatField() => ToSyntaxNode();
}
