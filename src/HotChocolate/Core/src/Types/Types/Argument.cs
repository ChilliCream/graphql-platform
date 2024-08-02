using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a field or directive argument.
/// </summary>
public class Argument : FieldBase, IInputField
{
    private Type _runtimeType = default!;

    /// <summary>
    /// Initializes a new <see cref="Argument"/>.
    /// </summary>
    /// <param name="definition">
    /// The argument definition.
    /// </param>
    /// <param name="index">
    /// The position of this argument within its field collection.
    /// </param>
    public Argument(ArgumentDefinition definition, int index)
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

        IsDeprecated = !string.IsNullOrEmpty(definition.DeprecationReason);
        DeprecationReason = definition.DeprecationReason;
    }

    /// <summary>
    /// Gets the type system member that declares this argument.
    /// </summary>
    public ITypeSystemMember DeclaringMember { get; private set; } = default!;

    /// <inheritdoc />
    public IInputType Type { get; private set; } = default!;

    /// <inheritdoc />
    public bool IsDeprecated { get; }

    /// <inheritdoc />
    public string? DeprecationReason { get; }

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

    protected sealed override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnCompleteField(context, declaringMember, (ArgumentDefinition)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ArgumentDefinition definition)
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

        Type = context.GetType<IInputType>(definition.Type!).EnsureInputType();
        _runtimeType = definition.GetRuntimeType()!;
        _runtimeType = CompleteRuntimeType(Type, _runtimeType, out var isOptional);
        DefaultValue = CompleteDefaultValue(context, definition, Type, Coordinate);
        IsOptional = isOptional;
        DeclaringMember = declaringMember;
    }

    /// <summary>
    /// Returns a string that represents the current argument.
    /// </summary>
    /// <returns>
    /// A string that represents the current argument.
    /// </returns>
    public override string ToString() => $"{Name}:{Type.Print()}";
}
