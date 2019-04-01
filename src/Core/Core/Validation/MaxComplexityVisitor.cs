using System;
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
                throw new ArgumentNullException(nameof(node));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (calculateComplexity == null)
            {
                throw new ArgumentNullException(
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
                throw new ArgumentNullException(nameof(document));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (FragmentDefinitionNode fragment in document.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            if (operation.TryGetOperationType(
                   context.Schema,
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
            foreach (FragmentDefinitionNode fragment in node.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            foreach (OperationDefinitionNode operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                if (operation.TryGetOperationType(
                    context.Schema,
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

            if(context.FragmentPath.Contains(node.Name.Value))
            {
                return;
            }

            if (newContext.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext
                    .AddFragment(node)
                    .SetTypeContext(type);
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
    }
}
