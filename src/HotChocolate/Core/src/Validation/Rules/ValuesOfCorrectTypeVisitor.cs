using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Literal values must be compatible with the type expected in the position
    /// they are found as per the coercion rules defined in the Type System
    /// chapter.
    ///
    /// http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type
    /// </summary>
    internal sealed class ValuesOfCorrectTypeVisitor
        : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            VariableNode valueNode,
            IDocumentValidatorContext context)
        {
            return Enter((IValueNode)valueNode, context);
        }

        protected override ISyntaxVisitorAction Enter(
            IValueNode valueNode,
            IDocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out IType currentType) &&
                currentType is IInputType locationType)
            {
                if (!IsInstanceOfType(context, locationType, valueNode))
                {
                    ISyntaxNode node = PeekLastDefiningSyntaxNode(context);

                    context.Errors.Add(
                        node.Kind switch
                        {
                            NodeKind.ObjectField =>
                                BuildFieldError(context, locationType, valueNode, node),
                            NodeKind.VariableDefinition =>
                                BuildVariableDefError(context, locationType, valueNode, node),
                            NodeKind.Argument =>
                                BuildArgumentError(context, locationType, valueNode, node),
                            _ => throw new InvalidOperationException()
                        });
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
            return Continue;
        }

        private IError BuildArgumentError(
            IDocumentValidatorContext context,
            IInputType locationType,
            IValueNode valueNode,
            ISyntaxNode node)
        {
            if (node is ArgumentNode definitionNode)
            {
                return ErrorBuilder.New()
                    .SetMessage(
                        "The specified argument value " +
                        "does not match the argument type.")
                    .AddLocation(valueNode)
                    .SetPath(context.CreateErrorPath())
                    .SetExtension("argument", definitionNode.Name.Value)
                    .SetExtension("argumentValue", valueNode)
                    .SetExtension("locationType", locationType.Visualize())
                    .SpecifiedBy("sec-Values-of-Correct-Type")
                    .Build();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private IError BuildVariableDefError(
            IDocumentValidatorContext context,
            IInputType locationType,
            IValueNode valueNode,
            ISyntaxNode node)
        {
            if (node is VariableDefinitionNode definitionNode)
            {
                return ErrorBuilder.New()
                    .SetMessage(
                        "The specified value type of variable " +
                        $"`{definitionNode.Variable.Name.Value}` " +
                        "does not match the variable type.")
                    .AddLocation(valueNode)
                    .SetPath(context.CreateErrorPath())
                    .SetExtension("variable", definitionNode.Variable.Name.Value)
                    .SetExtension("variableType", definitionNode.Type.ToString())
                    .SetExtension("locationType", locationType.Visualize())
                    .SpecifiedBy("sec-Values-of-Correct-Type")
                    .Build();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private IError BuildFieldError(
            IDocumentValidatorContext context,
            IInputType locationType,
            IValueNode valueNode,
            ISyntaxNode node)
        {
            if (context.InputFields.TryPeek(out IInputField field))
            {
                return ErrorBuilder.New()
                    .SetMessage(
                        "The specified value type of field " +
                        $"`{field.Name.Value}` " +
                        "does not match the field type.")
                    .AddLocation(valueNode)
                    .SetExtension("fieldName", field.Name.Value)
                    .SetExtension("fieldType", field.Type)
                    .SetExtension("locationType", locationType.Visualize())
                    .SetPath(context.CreateErrorPath())
                    .SpecifiedBy("sec-Values-of-Correct-Type")
                    .Build();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private ISyntaxNode PeekLastDefiningSyntaxNode(
            IDocumentValidatorContext context)
        {
            for (var i = context.Path.Count - 1; i > 0; i--)
            {
                if (context.Path[i].Kind == NodeKind.Argument ||
                    context.Path[i].Kind == NodeKind.ObjectField ||
                    context.Path[i].Kind == NodeKind.VariableDefinition)
                {
                    return context.Path[i];
                }
            }
            throw new InvalidOperationException();
        }

        private bool IsInstanceOfType(
            IDocumentValidatorContext context,
            IInputType inputType,
            IValueNode? value)
        {
            if (value is VariableNode v
                && context.Variables.TryGetValue(v.Name.Value, out VariableDefinitionNode? t)
                && t?.Type is ITypeNode typeNode)
            {
                return IsTypeCompatible(inputType, typeNode);
            }

            IInputType internalType = inputType;

            if (internalType.IsNonNullType())
            {
                internalType = (IInputType)internalType.InnerType();
            }

            if (internalType is ListType listType
                && listType.ElementType is IInputType elementType
                && value is ListValueNode list)
            {
                for (int i = 0; i < list.Items.Count; i++)
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
