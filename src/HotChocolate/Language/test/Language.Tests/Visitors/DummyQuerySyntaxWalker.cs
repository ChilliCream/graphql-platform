namespace HotChocolate.Language
{
    public class DummyQuerySyntaxWalker
        : QuerySyntaxWalker<object>
    {
        public bool VisitedAllNodes =>
            VisitedOperationDefinition
            && VisitedVariableDefinition
            && VisitedFragmentDefinition
            && VisitedSelectionSet
            && VisitedField
            && VisitedFragmentSpread
            && VisitedInlineFragment;

        public bool VisitedOperationDefinition { get; private set; }
        public bool VisitedVariableDefinition { get; private set; }
        public bool VisitedFragmentDefinition { get; private set; }
        public bool VisitedSelectionSet { get; private set; }
        public bool VisitedField { get; private set; }
        public bool VisitedFragmentSpread { get; private set; }
        public bool VisitedInlineFragment { get; private set; }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node,
            object context)
        {
            VisitedOperationDefinition = true;
            base.VisitOperationDefinition(node, context);
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            object context)
        {
            VisitedVariableDefinition = true;
            base.VisitVariableDefinition(node, context);
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            object context)
        {
            VisitedFragmentDefinition = true;
            base.VisitFragmentDefinition(node, context);
        }

        protected override void VisitSelectionSet(
            SelectionSetNode node,
            object context)
        {
            VisitedSelectionSet = true;
            base.VisitSelectionSet(node, context);
        }

        protected override void VisitField(
            FieldNode node,
            object context)
        {
            VisitedField = true;
            base.VisitField(node, context);
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            object context)
        {
            VisitedFragmentSpread = true;
            base.VisitFragmentSpread(node, context);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            object context)
        {
            VisitedInlineFragment = true;
            base.VisitInlineFragment(node, context);
        }
    }
}
