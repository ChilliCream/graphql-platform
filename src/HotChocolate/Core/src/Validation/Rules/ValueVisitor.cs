using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Every input field provided in an input object value must be defined in
/// the set of possible fields of that input object’s expected type.
///
/// http://spec.graphql.org/June2018/#sec-Input-Object-Field-Names
///
/// AND
///
/// Input objects must not contain more than one field of the same name,
/// otherwise an ambiguity would exist which includes an ignored portion
/// of syntax.
///
/// http://spec.graphql.org/June2018/#sec-Input-Object-Field-Uniqueness
///
/// AND
///
/// Input object fields may be required. Much like a field may have
/// required arguments, an input object may have required fields.
///
/// An input field is required if it has a non‐null type and does not have
/// a default value. Otherwise, the input object field is optional.
///
/// http://spec.graphql.org/June2018/#sec-Input-Object-Required-Fields
///
/// AND
///
/// Literal values must be compatible with the type expected in the position
/// they are found as per the coercion rules defined in the Type System
/// chapter.
///
/// http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type
///
/// AND
///
/// Oneof Input Objects require that exactly one field must be supplied and that
/// field must not be {null}.
///
/// DRAFT: https://github.com/graphql/graphql-spec/pull/825
/// </summary>
internal sealed class ValueVisitor : TypeDocumentValidatorVisitor
{
    public ValueVisitor()
        : base(new SyntaxVisitorOptions
        {
            VisitDirectives = true,
            VisitArguments = true
        })
    {
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (IntrospectionFields.TypeName.EqualsOrdinal(node.Name.Value))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is IComplexOutputType ot &&
            ot.Fields.TryGetField(node.Name.Value, out var of))
        {
            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        IDocumentValidatorContext context)
    {
        if (context.Schema.TryGetType<INamedType>(
            node.Type.NamedType().Name.Value, out var variableType))
        {
            context.Types.Push(variableType);
            return base.Enter(node, context);
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        VariableDefinitionNode node,
        IDocumentValidatorContext context)
    {
        context.Types.Pop();
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        IDocumentValidatorContext context)
    {
        if (context.Schema.TryGetDirectiveType(node.Name.Value, out var d))
        {
            context.Directives.Push(d);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        DirectiveNode node,
        IDocumentValidatorContext context)
    {
        context.Directives.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ArgumentNode node,
        IDocumentValidatorContext context)
    {
        if (context.Directives.TryPeek(out var directive))
        {
            if (directive.Arguments.TryGetField(node.Name.Value, out var argument))
            {
                context.InputFields.Push(argument);
                context.Types.Push(argument.Type);
                return Continue;
            }
            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        if (context.OutputFields.TryPeek(out var field))
        {
            if (field.Arguments.TryGetField(node.Name.Value, out var argument))
            {
                context.InputFields.Push(argument);
                context.Types.Push(argument.Type);
                return Continue;
            }
            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ArgumentNode node,
        IDocumentValidatorContext context)
    {
        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueNode node,
        IDocumentValidatorContext context)
    {
        context.Names.Clear();

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            if (!context.Names.Add(field.Name.Value))
            {
                context.ReportError(context.InputFieldAmbiguous(field));
            }
        }

        var namedType = context.Types.Peek().NamedType();

        if (namedType.IsLeafType())
        {
            return Enter((IValueNode)node, context);
        }

        if (namedType is InputObjectType inputObjectType)
        {
            if (inputObjectType.Directives.ContainsDirective(WellKnownDirectives.OneOf))
            {
                if (node.Fields.Count == 0 || node.Fields.Count > 1)
                {
                    context.ReportError(
                        context.OneOfMustHaveExactlyOneField(
                            node,
                            inputObjectType));
                }
                else
                {
                    var value = node.Fields[0];

                    if (inputObjectType.Fields.TryGetField(
                        value.Name.Value,
                        out var field))
                    {
                        if (value.Value.IsNull())
                        {
                            context.ReportError(
                                context.OneOfMustHaveExactlyOneField(
                                    node,
                                    inputObjectType));
                        }
                        else if (value.Value.Kind is SyntaxKind.Variable &&
                            !TryIsInstanceOfType(context, new NonNullType(field.Type), value.Value))
                        {
                            context.ReportError(
                                context.OneOfVariablesMustBeNonNull(
                                    node,
                                    field.Coordinate,
                                    ((VariableNode)value.Value).Name.Value));
                        }
                    }
                }
            }

            if (context.Names.Count >= inputObjectType.Fields.Count)
            {
                return Continue;
            }

            for (var i = 0; i < inputObjectType.Fields.Count; i++)
            {
                IInputField field = inputObjectType.Fields[i];
                if (field.Type.IsNonNullType() &&
                    field.DefaultValue.IsNull() &&
                    context.Names.Add(field.Name))
                {
                    context.ReportError(
                        context.FieldIsRequiredButNull(node, field.Name));
                }
            }
        }
        else
        {
            context.UnexpectedErrorsDetected = true;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is InputObjectType it &&
            it.Fields.TryGetField(node.Name.Value, out var field))
        {
            if (field.Type.IsNonNullType() &&
                node.Value.IsNull())
            {
                context.ReportError(
                    context.FieldIsRequiredButNull(node, field.Name));
            }

            context.InputFields.Push(field);
            context.Types.Push(field.Type);
            return Continue;
        }
        else
        {
            context.ReportError(context.InputFieldDoesNotExist(node));
            return Skip;
        }
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        IDocumentValidatorContext context)
    {
        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type))
        {
            if (type.NamedType().IsLeafType())
            {
                return Enter((IValueNode)node, context);
            }

            if (type.IsListType())
            {
                context.Types.Push(type.ElementType());
                return Continue;
            }
        }

        context.UnexpectedErrorsDetected = true;
        return Break;
    }

    protected override ISyntaxVisitorAction Leave(
        ListValueNode node,
        IDocumentValidatorContext context)
    {
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        IValueNode valueNode,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var currentType) &&
            currentType is IInputType locationType)
        {
            if (valueNode.IsNull() || TryIsInstanceOfType(context, locationType, valueNode))
            {
                return Skip;
            }

            if (TryPeekLastDefiningSyntaxNode(context, out var node) &&
                TryCreateValueError(context, locationType, valueNode, node, out var error))
            {
                context.ReportError(error);
                return Skip;
            }
        }
        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    private static bool TryCreateValueError(
        IDocumentValidatorContext context,
        IInputType locationType,
        IValueNode valueNode,
        ISyntaxNode node,
        [NotNullWhen(true)] out IError? error)
    {
        error = node.Kind switch
        {
            SyntaxKind.ObjectField =>
                context.InputFields.TryPeek(out var field)
                    ? context.FieldValueIsNotCompatible(field, locationType, valueNode)
                    : null,
            SyntaxKind.VariableDefinition =>
                context.VariableDefaultValueIsNotCompatible(
                    (VariableDefinitionNode)node, locationType, valueNode),
            SyntaxKind.Argument =>
                context.ArgumentValueIsNotCompatible(
                    (ArgumentNode)node, locationType, valueNode),
            _ => null
        };
        return error != null;
    }

    private bool TryPeekLastDefiningSyntaxNode(
        IDocumentValidatorContext context,
        [NotNullWhen(true)] out ISyntaxNode? node)
    {
        for (var i = context.Path.Count - 1; i > 0; i--)
        {
            if (context.Path[i].Kind == SyntaxKind.Argument ||
                context.Path[i].Kind == SyntaxKind.ObjectField ||
                context.Path[i].Kind == SyntaxKind.VariableDefinition)
            {
                node = context.Path[i];
                return true;
            }
        }
        node = null;
        return false;
    }

    private bool TryIsInstanceOfType(
        IDocumentValidatorContext context,
        IInputType inputType,
        IValueNode value)
    {
        try
        {
            return IsInstanceOfType(context, inputType, value);
        }
        // in the case a scalar IsInstanceOfType check is not done well an throws we will
        // catch this here and make sure that the validation fails correctly.
        catch
        {
            return false;
        }
    }

    private bool IsInstanceOfType(
        IDocumentValidatorContext context,
        IInputType inputType,
        IValueNode value)
    {
        if (value is VariableNode v
            && context.Variables.TryGetValue(v.Name.Value, out var t)
            && t.Type is { } typeNode)
        {
            return IsTypeCompatible(inputType, typeNode);
        }

        var internalType = inputType;

        if (internalType.Kind == TypeKind.NonNull)
        {
            internalType = (IInputType)internalType.InnerType();
            if (value.IsNull())
            {
                return false;
            }
        }

        if (internalType is ListType { ElementType: IInputType elementType, } &&
            value is ListValueNode list)
        {
            for (var i = 0; i < list.Items.Count; i++)
            {
                if (!IsInstanceOfType(context, elementType, list.Items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        if (value.Kind is SyntaxKind.NullValue)
        {
            return true;
        }

        if (inputType.Kind == TypeKind.NonNull && value.Kind == SyntaxKind.NullValue)
        {
            return false;
        }

        inputType = (INamedInputType)inputType.NamedType();

        if (inputType.IsEnumType())
        {
            if (value is StringValueNode)
            {
                return false;
            }

            return ((EnumType)inputType).IsInstanceOfType(value);
        }

        if (inputType.IsScalarType())
        {
            return ((ScalarType)inputType).IsInstanceOfType(value);
        }

        return value.Kind is SyntaxKind.ObjectValue;
    }

    private bool IsTypeCompatible(IType left, ITypeNode right)
    {
        if (left is NonNullType leftNonNull)
        {
            if (right is NonNullTypeNode rightNonNull)
            {
                return IsTypeCompatible(
                    leftNonNull.Type,
                    rightNonNull.Type);
            }
            return false;
        }

        if (right is NonNullTypeNode nonNull)
        {
            return IsTypeCompatible(left, nonNull.Type);
        }

        if (left is ListType leftList)
        {
            if (right is ListTypeNode rightList)
            {
                return IsTypeCompatible(
                    leftList.ElementType,
                    rightList.Type);
            }
            return false;
        }

        if (left is INamedType leftNamedType
            && right is NamedTypeNode rightNamedType)
        {
            return leftNamedType.Name.EqualsOrdinal(rightNamedType.Name.Value);
        }

        return false;
    }
}
