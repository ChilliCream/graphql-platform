using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Every input field provided in an input object value must be defined in
/// the set of possible fields of that input object’s expected type.
///
/// https://spec.graphql.org/June2018/#sec-Input-Object-Field-Names
///
/// AND
///
/// Input objects must not contain more than one field of the same name,
/// otherwise an ambiguity would exist which includes an ignored portion
/// of syntax.
///
/// https://spec.graphql.org/June2018/#sec-Input-Object-Field-Uniqueness
///
/// AND
///
/// Input object fields may be required. Much like a field may have
/// required arguments, an input object may have required fields.
///
/// An input field is required if it has a non‐null type and does not have
/// a default value. Otherwise, the input object field is optional.
///
/// https://spec.graphql.org/June2018/#sec-Input-Object-Required-Fields
///
/// AND
///
/// Literal values must be compatible with the type expected in the position
/// they are found as per the coercion rules defined in the Type System
/// chapter.
///
/// https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type
///
/// AND
///
/// OneOf Input Objects require that exactly one field must be supplied and that
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
        DocumentNode node,
        DocumentValidatorContext context)
    {
        context.Features.GetOrSet<ValueVisitorFeature>().Reset();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IComplexTypeDefinition ot
            && ot.Fields.TryGetField(node.Name.Value, out var of))
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
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.Types.TryGetType<ITypeDefinition>(
            node.Type.NamedType().Name.Value,
            out var variableType))
        {
            context.Types.Push(node.Type.RewriteToType(variableType));
            return base.Enter(node, context);
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        VariableDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.DirectiveDefinitions.TryGetDirective(node.Name.Value, out var d))
        {
            context.Directives.Push(d);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        DirectiveNode node,
        DocumentValidatorContext context)
    {
        context.Directives.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ArgumentNode node,
        DocumentValidatorContext context)
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
        DocumentValidatorContext context)
    {
        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueNode node,
        DocumentValidatorContext context)
    {
        var inputFieldNames = context.Features.GetRequired<ValueVisitorFeature>().InputFieldNames;
        inputFieldNames.Clear();

        for (var i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            if (!inputFieldNames.Add(field.Name.Value))
            {
                context.ReportError(context.InputFieldAmbiguous(field));
            }
        }

        var namedType = context.Types.Peek().NamedType();

        if (namedType.IsLeafType())
        {
            return Enter((IValueNode)node, context);
        }

        if (namedType is IInputObjectTypeDefinition inputObjectType)
        {
            if (inputObjectType.Directives.ContainsName(DirectiveNames.OneOf.Name))
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
                        else if (value.Value.Kind is SyntaxKind.Variable
                            && !TryIsInstanceOfType(context, new NonNullType(field.Type), value.Value))
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

            if (inputFieldNames.Count >= inputObjectType.Fields.Count)
            {
                return Continue;
            }

            for (var i = 0; i < inputObjectType.Fields.Count; i++)
            {
                var field = inputObjectType.Fields[i];
                if (field.Type.IsNonNullType()
                    && field.DefaultValue.IsNull()
                    && inputFieldNames.Add(field.Name))
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
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IInputObjectTypeDefinition it
            && it.Fields.TryGetField(node.Name.Value, out var field))
        {
            if (field.Type.IsNonNullType()
                && node.Value.IsNull())
            {
                context.ReportError(
                    context.FieldIsRequiredButNull(node, field.Name));
            }

            context.InputFields.Push(field);
            context.Types.Push(field.Type);
            return Continue;
        }

        context.ReportError(context.InputFieldDoesNotExist(node));
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        DocumentValidatorContext context)
    {
        context.InputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueNode node,
        DocumentValidatorContext context)
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
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        IValueNode valueNode,
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var currentType)
            && currentType is IInputType locationType)
        {
            if (valueNode.IsNull() || TryIsInstanceOfType(context, locationType, valueNode))
            {
                return Skip;
            }

            if (TryPeekLastDefiningSyntaxNode(context, out var node)
                && TryCreateValueError(context, locationType, valueNode, node, out var error))
            {
                context.ReportError(error);
                return Skip;
            }
        }
        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    private static bool TryCreateValueError(
        DocumentValidatorContext context,
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

    private static bool TryPeekLastDefiningSyntaxNode(
        DocumentValidatorContext context,
        [NotNullWhen(true)] out ISyntaxNode? node)
    {
        for (var i = context.Path.Count - 1; i > 0; i--)
        {
            if (context.Path[i].Kind == SyntaxKind.Argument
                || context.Path[i].Kind == SyntaxKind.ObjectField
                || context.Path[i].Kind == SyntaxKind.VariableDefinition)
            {
                node = context.Path[i];
                return true;
            }
        }
        node = null;
        return false;
    }

    private static bool TryIsInstanceOfType(
        DocumentValidatorContext context,
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

    private static bool IsInstanceOfType(
        DocumentValidatorContext context,
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

        if (internalType is ListType { ElementType: IInputType elementType }
            && value is ListValueNode list)
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

        if (inputType.Kind == TypeKind.NonNull && value.Kind == SyntaxKind.NullValue)
        {
            return false;
        }

        if (value.Kind is SyntaxKind.NullValue)
        {
            return true;
        }

        inputType = (IInputType)inputType.AsTypeDefinition();

        if (inputType.IsEnumType())
        {
            if (value is EnumValueNode enumValue)
            {
                return inputType.ExpectEnumType().Values.ContainsName(enumValue.Value);
            }

            return false;
        }

        if (inputType is IScalarTypeDefinition scalarType)
        {
            return scalarType.IsValueCompatible(value);
        }

        return value.Kind is SyntaxKind.ObjectValue;
    }

    private static bool IsTypeCompatible(IType left, ITypeNode right)
    {
        if (left is NonNullType leftNonNull)
        {
            if (right is NonNullTypeNode rightNonNull)
            {
                return IsTypeCompatible(leftNonNull.NullableType, rightNonNull.Type);
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
                return IsTypeCompatible(leftList.ElementType, rightList.Type);
            }
            return false;
        }

        if (left is ITypeDefinition leftNamedType
            && right is NamedTypeNode rightNamedType)
        {
            return leftNamedType.Name.Equals(rightNamedType.Name.Value, StringComparison.Ordinal);
        }

        return false;
    }

    private sealed class ValueVisitorFeature : ValidatorFeature
    {
        public HashSet<string> InputFieldNames { get; } = [];

        protected internal override void Reset() => InputFieldNames.Clear();
    }
}
