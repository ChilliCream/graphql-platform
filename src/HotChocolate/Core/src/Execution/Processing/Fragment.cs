using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public class Fragment : IFragment
    {
        private readonly SelectionIncludeCondition? _includeCondition;

        public Fragment(
            IObjectType typeCondition,
            InlineFragmentNode inlineFragment,
            ISelectionSet selectionSet,
            bool internalFragment,
            SelectionIncludeCondition? includeCondition)
        {
            TypeCondition = typeCondition;
            SyntaxNode = inlineFragment;
            SelectionSet = selectionSet;
            Directives = inlineFragment.Directives;
            IsInternal = internalFragment;
            IsConditional = includeCondition is not null;
            _includeCondition = includeCondition;

            InclusionKind = internalFragment
                ? SelectionInclusionKind.Internal
                : SelectionInclusionKind.Always;

            if (IsConditional)
            {
                InclusionKind = internalFragment
                    ? SelectionInclusionKind.InternalConditional
                    : SelectionInclusionKind.Conditional;
            }
        }

        public Fragment(
            IObjectType typeCondition,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition,
            ISelectionSet selectionSet,
            bool internalFragment,
            SelectionIncludeCondition? includeCondition)
        {
            TypeCondition = typeCondition;
            SyntaxNode = fragmentDefinition;
            SelectionSet = selectionSet;
            Directives = fragmentSpread.Directives;
            IsInternal = internalFragment;
            IsConditional = includeCondition is not null;
            _includeCondition = includeCondition;

            InclusionKind = internalFragment
                ? SelectionInclusionKind.Internal
                : SelectionInclusionKind.Always;

            if (IsConditional)
            {
                InclusionKind = internalFragment
                    ? SelectionInclusionKind.InternalConditional
                    : SelectionInclusionKind.Conditional;
            }
        }

        public IObjectType TypeCondition { get; }

        public ISyntaxNode SyntaxNode { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public ISelectionSet SelectionSet { get; }

        public string? GetLabel(IVariableValueCollection variables) =>
            Directives.GetDeferDirective(variables)?.Label;

        public SelectionInclusionKind InclusionKind { get; }

        public bool IsInternal { get; }

        public bool IsConditional { get; }

        public bool IsIncluded(IVariableValueCollection variableValues, bool allowInternals = false)
        {
            return InclusionKind switch
            {
                SelectionInclusionKind.Always => true,
                SelectionInclusionKind.Conditional => EvaluateConditions(variableValues),
                SelectionInclusionKind.Internal => allowInternals,
                SelectionInclusionKind.InternalConditional =>
                    allowInternals && EvaluateConditions(variableValues),
                _ => throw new NotSupportedException()
            };
        }

        private bool EvaluateConditions(IVariableValueCollection variableValues) =>
            _includeCondition?.IsTrue(variableValues) ?? true;
    }
}
