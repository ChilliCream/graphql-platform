using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IFragment : IOptionalSelection
    {
        INamedOutputType TypeCondition { get; }

        ISyntaxNode SyntaxNode { get; }

        ISelectionSet SelectionSet { get; }
    }

    public class Fragment : IFragment
    {
        private readonly SelectionIncludeCondition? _includeCondition;

        public Fragment(
            INamedOutputType typeCondition,
            ISyntaxNode syntaxNode,
            ISelectionSet selectionSet,
            bool internalFragment,
            SelectionIncludeCondition? includeCondition)
        {
            TypeCondition = typeCondition;
            SyntaxNode = syntaxNode;
            SelectionSet = selectionSet;
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

        public INamedOutputType TypeCondition { get; }

        public ISyntaxNode SyntaxNode { get; }

        public ISelectionSet SelectionSet { get; }

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
