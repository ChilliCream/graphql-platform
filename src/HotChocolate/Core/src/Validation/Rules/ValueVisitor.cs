using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation.Rules
{
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
            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                return Skip;
            }

            if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField of))
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
            if (context.Schema.TryGetType(
                node.Type.NamedType().Name.Value, out INamedType variableType))
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
            if (context.Schema.TryGetDirectiveType(node.Name.Value, out DirectiveType? d))
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
            if (context.Directives.TryPeek(out DirectiveType directive))
            {
                if (directive.Arguments.TryGetField(node.Name.Value, out Argument argument))
                {
                    context.InputFields.Push(argument);
                    context.Types.Push(argument.Type);
                    return Continue;
                }
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }

            if (context.OutputFields.TryPeek(out IOutputField field))
            {
                if (field.Arguments.TryGetField(node.Name.Value, out IInputField argument))
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
                ObjectFieldNode field = node.Fields[i];
                if (!context.Names.Add(field.Name.Value))
                {
                    context.Errors.Add(context.InputFieldAmbiguous(field));
                }
            }

            INamedType namedType = context.Types.Peek().NamedType();

            if (namedType.IsLeafType())
            {
                return Enter((IValueNode)node, context);
            }

            if (namedType is InputObjectType inputObjectType)
            {
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
                        context.Errors.Add(
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
            if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is InputObjectType it &&
                it.Fields.TryGetField(node.Name.Value, out InputField field))
            {
                if (field.Type.IsNonNullType() &&
                    node.Value.IsNull())
                {
                    context.Errors.Add(
                        context.FieldIsRequiredButNull(node, field.Name));
                }

                context.InputFields.Push(field);
                context.Types.Push(field.Type);
                return Continue;
            }
            else
            {
                context.Errors.Add(context.InputFieldDoesNotExist(node));
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
            if (context.Types.TryPeek(out IType? type))
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
            if (context.Types.TryPeek(out IType currentType) &&
                currentType is IInputType locationType)
            {
                if (IsInstanceOfType(context, locationType, valueNode))
                {
                    return Skip;
                }

                if (TryPeekLastDefiningSyntaxNode(context, out ISyntaxNode? node) &&
                    TryCreateValueError(context, locationType, valueNode, node, out IError? error))
                {
                    context.Errors.Add(error);
                    return Skip;
                }
            }
            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        private bool TryCreateValueError(
            IDocumentValidatorContext context,
            IInputType locationType,
            IValueNode valueNode,
            ISyntaxNode node,
            [NotNullWhen(true)]out IError? error)
        {
            error = node.Kind switch
            {
                SyntaxKind.ObjectField =>
                    context.InputFields.TryPeek(out IInputField field)
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

        private bool IsInstanceOfType(
            IDocumentValidatorContext context,
            IInputType inputType,
            IValueNode? value)
        {
            if (value is VariableNode v
                && context.Variables.TryGetValue(v.Name.Value, out VariableDefinitionNode? t)
                && t?.Type is { } typeNode)
            {
                return IsTypeCompatible(inputType, typeNode);
            }

            IInputType internalType = inputType;

            if (internalType.IsNonNullType())
            {
                internalType = (IInputType)internalType.InnerType();
                if (value.IsNull())
                {
                    return false;
                }
            }

            if (internalType is ListType listType
                && listType.ElementType is IInputType elementType
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

            return internalType.IsInstanceOfType(value);
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
                return leftNamedType.Name.Equals(rightNamedType.Name.Value);
            }

            return false;
        }
    }
}
