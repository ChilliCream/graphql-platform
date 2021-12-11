using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types;

public class InputField : FieldBase<InputFieldDefinition>, IInputField
{
    private Type _runtimeType = default!;

    public InputField(InputFieldDefinition definition, int index)
        : base(definition, index)
    {
        DefaultValue = definition.DefaultValue;
        Property = definition.Property;

        IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();
        Formatter = formatters.Count switch
        {
            0 => null,
            1 => formatters[0],
            _ => new AggregateInputValueFormatter(formatters)
        };
    }

    /// <summary>
    /// The associated syntax node from the GraphQL SDL.
    /// </summary>
    public new InputValueDefinitionNode? SyntaxNode =>
        (InputValueDefinitionNode?)base.SyntaxNode;

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

    /// <summary>
    /// If this field is bound to a property on a concrete model,
    /// then this property exposes this property.
    /// </summary>
    protected internal PropertyInfo? Property { get; }

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldDefinition definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        Type = context.GetType<IInputType>(definition.Type!);
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
