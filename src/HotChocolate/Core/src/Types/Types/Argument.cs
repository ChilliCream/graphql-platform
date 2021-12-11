using System;
using System.Collections.Generic;
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
public class Argument : FieldBase<ArgumentDefinition>, IInputField
{
    private Type _runtimeType = default!;

    public Argument(ArgumentDefinition definition, int index)
        : base(definition, index)
    {
        DefaultValue = definition.DefaultValue;

        IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();

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
    /// The associated syntax node from the GraphQL SDL.
    /// </summary>
    public new InputValueDefinitionNode? SyntaxNode
        => (InputValueDefinitionNode?)base.SyntaxNode;

    /// <summary>
    /// Gets the type system member that declares this argument.
    /// </summary>
    public ITypeSystemMember DeclaringMember { get; private set; } = default!;

    /// <inheritdoc />
    public IInputType Type { get; private set; } = default!;

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

    protected override void OnCompleteField(
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
                .SetExtension("name", definition.Name.ToString())
                .Build());
            return;
        }

        base.OnCompleteField(context, declaringMember, definition);

        Type = context.GetType<IInputType>(definition.Type!);
        _runtimeType = definition.RuntimeType ?? definition.Parameter?.ParameterType!;
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
