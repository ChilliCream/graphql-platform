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

        protected override IDocumentValidatorContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
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
                    context.IsInError.Push(false);
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
                        context.OutputFields.Push(of);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;

                case NodeKind.InlineFragment:
                    var inlineFragment = ((InlineFragmentNode)node);
                    if (inlineFragment.TypeCondition is null)
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(false);
                    }
                    else if (context.Schema.TryGetType(
                        inlineFragment.TypeCondition.Name.Value,
                        out namedOutputType))
                    {
                        context.Types.Push(namedOutputType);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;

                case NodeKind.FragmentDefinition:
                    var fragmentDefinition = ((FragmentDefinitionNode)node);
                    if (context.Schema.TryGetType(
                        fragmentDefinition.TypeCondition.Name.Value,
                        out namedOutputType))
                    {
                        context.Types.Push(namedOutputType);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;

                case NodeKind.Argument:
                    string argName = ((ArgumentNode)node).Name.Value;

                    if (context.Directives.TryPeek(out directiveType) &&
                        directiveType.Arguments.TryGetField(argName, out inputField))
                    {
                        context.InputFields.Push(inputField);
                        context.IsInError.Push(false);
                    }
                    else if (context.OutputFields.TryPeek(out outputField) &&
                        outputField.Arguments.TryGetField(argName, out inputField))
                    {
                        context.InputFields.Push(inputField);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;

                case NodeKind.ObjectField:
                    string fieldName = ((ObjectFieldNode)node).Name.Value;

                    if (context.Types.Count > 0 &&
                        context.Types.Peek().NamedType() is InputObjectType inputType &&
                        inputType.Fields.TryGetField(fieldName, out inputField))
                    {
                        context.InputFields.Push(inputField);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;

                case NodeKind.Directive:
                    string directiveName = ((DirectiveNode)node).Name.Value;

                    if (context.Schema.TryGetDirectiveType(directiveName, out DirectiveType d))
                    {
                        context.Directives.Push(d);
                        context.IsInError.Push(false);
                    }
                    else
                    {
                        context.Types.Push(context.Types.Peek());
                        context.IsInError.Push(true);
                    }
                    break;
            }

            return base.OnBeforeEnter(node, parent, context);
        }

        protected override IDocumentValidatorContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IDocumentValidatorContext context)
        {
            IOutputField? outputField;
            IInputField? inputField;

            switch (node.Kind)
            {
                case NodeKind.Field:
                    if (context.OutputFields.TryPeek(out outputField) &&
                        outputField.Name.Equals(((FieldNode)node).Name.Value))
                    {
                        context.Types.Push(outputField.Type);
                    }
                    break;

                case NodeKind.ObjectField:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ObjectFieldNode)node).Name.Value))
                    {
                        context.Types.Push(inputField.Type);
                    }
                    break;

                case NodeKind.Argument:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ArgumentNode)node).Name.Value))
                    {
                        context.Types.Push(inputField.Type);
                    }
                    break;
            }

            return base.OnAfterEnter(node, parent, context);
        }

        protected override IDocumentValidatorContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IDocumentValidatorContext context)
        {
            IOutputField? outputField;
            IInputField? inputField;

            switch (node.Kind)
            {
                case NodeKind.Field:
                    if (context.OutputFields.TryPeek(out outputField) &&
                        outputField.Name.Equals(((FieldNode)node).Name.Value))
                    {
                        context.Types.TryPop(out _);
                    }
                    break;

                case NodeKind.ObjectField:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ObjectFieldNode)node).Name.Value))
                    {
                        context.Types.TryPop(out _);
                    }
                    break;

                case NodeKind.Argument:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ArgumentNode)node).Name.Value))
                    {
                        context.Types.TryPop(out _);
                    }
                    break;
            }

            return base.OnBeforeLeave(node, parent, context);
        }

        protected override IDocumentValidatorContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IDocumentValidatorContext context)
        {
            IOutputField? outputField;
            IInputField? inputField;

            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                    context.Types.Pop();
                    context.Variables.Clear();
                    context.IsInError.Pop();
                    break;

                case NodeKind.InlineFragment:
                case NodeKind.FragmentDefinition:
                    context.Types.Pop();
                    context.IsInError.Pop();
                    break;

                case NodeKind.Field:
                    if (context.OutputFields.TryPeek(out outputField) &&
                        outputField.Name.Equals(((FieldNode)node).Name.Value))
                    {
                        context.OutputFields.Pop();
                    }
                    context.IsInError.Pop();
                    break;

                case NodeKind.ObjectField:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ObjectFieldNode)node).Name.Value))
                    {
                        context.InputFields.Pop();
                    }
                    context.IsInError.Pop();
                    break;

                case NodeKind.Argument:
                    if (context.InputFields.TryPeek(out inputField) &&
                        inputField.Name.Equals(((ArgumentNode)node).Name.Value))
                    {
                        context.InputFields.Pop();
                    }
                    context.IsInError.Pop();
                    break;

                case NodeKind.Directive:
                    context.Directives.Pop();
                    context.IsInError.Pop();
                    break;
            }

            return base.OnAfterLeave(node, parent, context);
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
