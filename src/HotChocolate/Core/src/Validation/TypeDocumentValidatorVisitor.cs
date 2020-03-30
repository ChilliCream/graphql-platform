using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class TypeDocumentValidatorVisitor
       : DocumentValidatorVisitor
    {
        protected TypeDocumentValidatorVisitor()
        {
        }

        protected override IDocumentValidatorContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            INamedOutputType? namedOutputType;
            IOutputField? outputField;
            IInputField? inputField;
            DirectiveType? directiveType;
            IType? type;

            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                    var operation = (OperationDefinitionNode)node;
                    context.Types.Push(GetOperationType(context.Schema, operation.Operation));
                    break;

                case NodeKind.VariableDefinition:
                    var variable = (VariableDefinitionNode)node;
                    context.Variables[variable.Variable.Name.Value] = variable;
                    break;

                case NodeKind.Field:
                    var field = ((FieldNode)node);
                    if (context.Types.TryPeek(out type) &&
                        type.NamedType() is IComplexOutputType ot &&
                        ot.Fields.TryGetField(field.Name.Value, out IOutputField of))
                    {
                        context.Types.Push(of.Type);
                        context.OutputFields.Push(of);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.InlineFragment:
                    var inlineFragment = ((InlineFragmentNode)node);
                    if (inlineFragment.TypeCondition is { } tc &&
                        context.Schema.TryGetType(tc.Name.Value, out namedOutputType))
                    {
                        context.Types.Push(namedOutputType);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.FragmentDefinition:
                    var fragmentDefinition = ((FragmentDefinitionNode)node);
                    if (context.Schema.TryGetType(
                        fragmentDefinition.TypeCondition.Name.Value,
                        out namedOutputType))
                    {
                        context.Types.Push(namedOutputType);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.Argument:
                    string argName = ((ArgumentNode)node).Name.Value;

                    if (context.Directives.TryPeek(out directiveType) &&
                        directiveType.Arguments.TryGetField(argName, out inputField))
                    {
                        context.Types.Push(inputField.Type);
                        context.InputFields.Push(inputField);
                    }
                    else if (context.OutputFields.TryPeek(out outputField) &&
                        outputField.Arguments.TryGetField(argName, out inputField))
                    {
                        context.Types.Push(inputField.Type);
                        context.InputFields.Push(inputField);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.ObjectField:
                    string fieldName = ((ObjectFieldNode)node).Name.Value;

                    if (context.Types.Count > 0 &&
                        context.Types.Peek().NamedType() is InputObjectType inputType &&
                        inputType.Fields.TryGetField(fieldName, out inputField))
                    {
                        context.Types.Push(inputField.Type);
                        context.InputFields.Push(inputField);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.Directive:
                    string directiveName = ((DirectiveNode)node).Name.Value;

                    if (context.Schema.TryGetDirectiveType(directiveName, out DirectiveType d))
                    {
                        context.Directives.Push(d);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;
            }

            return base.OnAfterEnter(node, parent, ancestors, context);
        }

        protected override IDocumentValidatorContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                    context.Types.Pop();
                    context.Variables.Clear();
                    break;

                case NodeKind.InlineFragment:
                case NodeKind.FragmentDefinition:
                case NodeKind.Argument:
                case NodeKind.ObjectField:
                    context.Types.Pop();
                    break;

                case NodeKind.FieldDefinition:
                    context.Types.Pop();
                    context.OutputFields.Pop();
                    break;

                case NodeKind.Directive:
                    context.Directives.Pop();
                    break;
            }

            return base.OnBeforeLeave(node, parent, ancestors, context);
        }

        private static ObjectType GetOperationType(
            ISchema schema,
            OperationType operation)
        {
            switch (operation)
            {
                case Language.OperationType.Query:
                    return schema.QueryType;
                case Language.OperationType.Mutation:
                    return schema.MutationType;
                case Language.OperationType.Subscription:
                    return schema.SubscriptionType;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
