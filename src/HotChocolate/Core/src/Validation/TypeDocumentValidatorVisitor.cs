using System;
using System.Collections.Generic;
using System.Linq;
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
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            INamedOutputType? namedOutputType;
            IInputField? inputField;

            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                    var operation = (OperationDefinitionNode)node;
                    ObjectType type = GetOperationType(context.Schema, operation.Operation);
                    context.OutputField = null;
                    break;

                case NodeKind.FieldDefinition:
                    var field = ((FieldDefinitionNode)node);
                    if (context.Types.Count > 0 &&
                        context.Types.Peek().NamedType() is IComplexOutputType ot &&
                        ot.Fields.TryGetField(field.Name.Value, out IOutputField of))
                    {
                        context.Types.Push(of.Type);
                        context.OutputField = of;
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
                        context.OutputField = null;
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
                        context.OutputField = null;
                        context.Types.Push(namedOutputType);
                    }
                    else
                    {
                        context.IsInError = true;
                    }
                    break;

                case NodeKind.Argument:
                    string argName = ((ArgumentNode)node).Name.Value;

                    if (context.OutputField is { } &&
                        context.OutputField.Arguments.TryGetField(argName, out inputField))
                    {
                        context.Types.Push(inputField.Type);
                    }
                    else if (context.Directives.Count > 0 &&
                        context.Directives.Peek().Arguments.TryGetField(argName, out inputField))
                    {
                        context.Types.Push(inputField.Type);
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

            return base.OnBeforeEnter(node, parent, ancestors, context);
        }

        protected override IDocumentValidatorContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.OperationDefinition:
                case NodeKind.InlineFragment:
                case NodeKind.FragmentDefinition:
                case NodeKind.Argument:
                case NodeKind.ObjectField:
                    context.Types.Pop();
                    break;

                case NodeKind.FieldDefinition:
                    context.Types.Pop();
                    context.OutputField = null;
                    break;

                case NodeKind.Directive:
                    context.Directives.Pop();
                    break;
            }

            return base.OnBeforeEnter(node, parent, ancestors, context);
        }

        protected override IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Document:
                    return ((DocumentNode)node).Definitions.Where(t =>
                        t.Kind != NodeKind.FragmentDefinition);

                case NodeKind.FragmentSpread:
                    return GetFragmentSpreadChildren((FragmentSpreadNode)node, context);

                default:
                    return node.GetNodes();
            }
        }

        private static IEnumerable<ISyntaxNode> GetFragmentSpreadChildren(
            FragmentSpreadNode fragmentSpread,
            IDocumentValidatorContext context)
        {
            foreach (ISyntaxNode child in fragmentSpread.GetNodes())
            {
                yield return child;
            }

            if (context.Fragments.TryGetValue(
                fragmentSpread.Name.Value,
                out FragmentDefinitionNode? fragment))
            {
                yield return fragment;
            }
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
