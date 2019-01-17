using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class MaxComplexityVisitor
        : QuerySyntaxWalker<MaxComplexityVisitorContext>
    {
        protected override bool VisitFragmentDefinitions => false;

        public int Visit(
            DocumentNode node,
            ISchema schema,
            ComplexityCalculation calculateComplexity)
        {
            if (node == null)
            {
                throw new System.ArgumentNullException(nameof(node));
            }

            if (schema == null)
            {
                throw new System.ArgumentNullException(nameof(schema));
            }

            if (calculateComplexity == null)
            {
                throw new System.ArgumentNullException(
                    nameof(calculateComplexity));
            }

            var context = MaxComplexityVisitorContext
                .New(schema, calculateComplexity);
            Visit(node, context);
            return context.MaxComplexity;
        }

        public int Visit(
            DocumentNode document,
            OperationDefinitionNode operation,
            MaxComplexityVisitorContext context)
        {
            if (document == null)
            {
                throw new System.ArgumentNullException(nameof(document));
            }

            if (operation == null)
            {
                throw new System.ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            foreach (var fragment in document.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            if (TryGetOperationType(
                   context.Schema,
                   operation.Operation,
                   out ObjectType objectType))
            {
                VisitOperationDefinition(
                    operation,
                    context.SetTypeContext(objectType));
            }

            return context.MaxComplexity;
        }

        protected override void VisitDocument(
            DocumentNode node,
            MaxComplexityVisitorContext context)
        {
            foreach (var fragment in node.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            foreach (var operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                if (TryGetOperationType(
                    context.Schema,
                    operation.Operation,
                    out ObjectType objectType))
                {
                    VisitOperationDefinition(
                        operation,
                        context
                            .CreateScope()
                            .SetTypeContext(objectType));
                }
            }
        }

        protected override void VisitField(
            FieldNode node,
            MaxComplexityVisitorContext context)
        {
            MaxComplexityVisitorContext newContext = context;

            if (context.TypeContext is IComplexOutputType type
                && type.Fields.TryGetField(node.Name.Value,
                    out IOutputField fieldDefinition))
            {
                newContext = newContext.AddField(fieldDefinition, node);

                if (fieldDefinition.Type.NamedType() is IComplexOutputType ct)
                {
                    newContext = newContext.SetTypeContext(ct);
                }

                base.VisitField(node, newContext);
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            MaxComplexityVisitorContext context)
        {
            base.VisitFragmentSpread(node, context);

            if (context.Fragments.TryGetValue(node.Name.Value,
                out FragmentDefinitionNode fragment))
            {
                VisitFragmentDefinition(fragment, context);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            MaxComplexityVisitorContext context)
        {
            MaxComplexityVisitorContext newContext = context;

            if (newContext.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext.SetTypeContext(type);
            }

            base.VisitFragmentDefinition(node, newContext);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            MaxComplexityVisitorContext context)
        {
            MaxComplexityVisitorContext newContext = context;

            if (newContext.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext.SetTypeContext(type);
            }

            base.VisitInlineFragment(node, newContext);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            MaxComplexityVisitorContext context)
        {
            if (!context.FragmentPath.Contains(node.Name.Value))
            {
                base.VisitFieldDefinition(node, context);
            }
        }

        private static bool TryGetOperationType(
            ISchema schema,
            OperationType operation,
            out ObjectType objectType)
        {
            switch (operation)
            {
                case OperationType.Query:
                    objectType = schema.QueryType;
                    break;

                case OperationType.Mutation:
                    objectType = schema.MutationType;
                    break;

                case Language.OperationType.Subscription:
                    objectType = schema.SubscriptionType;
                    break;

                default:
                    objectType = null;
                    break;
            }

            return objectType != null;
        }
    }
}
