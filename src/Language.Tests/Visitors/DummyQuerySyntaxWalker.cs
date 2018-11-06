using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class DummyQuerySyntaxWalker
        : QuerySyntaxWalker
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
            OperationDefinitionNode node)
        {
            VisitedOperationDefinition = true;
            base.VisitOperationDefinition(node);
        }

        protected override void VisitVariableDefinition(
           VariableDefinitionNode node)
        {
            VisitedVariableDefinition = true;
            base.VisitVariableDefinition(node);
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node)
        {
            VisitedFragmentDefinition = true;
            base.VisitFragmentDefinition(node);
        }

        protected override void VisitSelectionSet(SelectionSetNode node)
        {
            VisitedSelectionSet = true;
            base.VisitSelectionSet(node);
        }

        protected override void VisitField(FieldNode node)
        {
            VisitedField = true;
            base.VisitField(node);
        }

        protected override void VisitFragmentSpread(FragmentSpreadNode node)
        {
            VisitedFragmentSpread = true;
            base.VisitFragmentSpread(node);
        }

        protected override void VisitInlineFragment(InlineFragmentNode node)
        {
            VisitedInlineFragment = true;
            base.VisitInlineFragment(node);
        }
    }
}
