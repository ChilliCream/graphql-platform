namespace HotChocolate.Language
{
    public class QuerySyntaxWalker<TContext>
        : SyntaxWalkerBase<DocumentNode, TContext>
    {
        protected virtual bool VisitFragmentDefinitions => true;

        public override void Visit(
            DocumentNode node,
            TContext context)
        {
            if (node != null)
            {
                VisitDocument(node, context);
            }
        }

        protected override void VisitDocument(
            DocumentNode node,
            TContext context)
        {
            VisitMany(node.Definitions, context, VisitDefinition);
        }

        protected virtual void VisitDefinition(
            IDefinitionNode node,
            TContext context)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    VisitOperationDefinition(value, context);
                    break;
                case FragmentDefinitionNode value:
                    if (VisitFragmentDefinitions)
                    {
                        VisitFragmentDefinition(value, context);
                    }
                    break;
                default:
                    VisitUnsupportedDefinitions(node, context);
                    break;
            }
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node,
            TContext context)
        {
            if (node.Name != null)
            {
                VisitName(node.Name, context);

                VisitMany(
                    node.VariableDefinitions,
                    context,
                    VisitVariableDefinition);

                VisitMany(
                    node.Directives,
                    context,
                    VisitDirective);
            }

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
        }

        protected override void VisitVariableDefinition(
           VariableDefinitionNode node,
           TContext context)
        {
            VisitVariable(node.Variable, context);
            VisitType(node.Type, context);


            if (node.DefaultValue != null)
            {
                VisitValue(node.DefaultValue, context);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            TContext context)
        {
            VisitName(node.Name, context);

            VisitMany(
                node.VariableDefinitions,
                context,
                VisitVariableDefinition);

            VisitNamedType(node.TypeCondition, context);

            VisitMany(
                node.Directives,
                context,
                VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
        }

        protected override void VisitSelectionSet(
            SelectionSetNode node,
            TContext context)
        {
            VisitMany(
                node.Selections,
                context,
                VisitSelection);
        }

        protected override void VisitField(
            FieldNode node,
            TContext context)
        {
            if (node.Alias != null)
            {
                VisitName(node.Alias, context);
            }

            VisitName(node.Name, context);

            VisitMany(
                node.Arguments,
                context,
                VisitArgument);

            VisitMany(
                node.Directives,
                context,
                VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            TContext context)
        {
            VisitName(node.Name, context);

            VisitMany(
                node.Directives, 
                context, 
                VisitDirective);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            TContext context)
        {
            if (node.TypeCondition != null)
            {
                VisitNamedType(node.TypeCondition, context);
            }

            VisitMany(
                node.Directives, 
                context, 
                VisitDirective);

            if (node.SelectionSet != null)
            {
                VisitSelectionSet(node.SelectionSet, context);
            }
        }
    }
}
