using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadsMustNotFormCyclesVisitor
        : QueryVisitorErrorBase
    {
        private readonly HashSet<FragmentDefinitionNode> _visited =
            new HashSet<FragmentDefinitionNode>();

        private bool _cycleDetected;

        public FragmentSpreadsMustNotFormCyclesVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitOperationDefinitions(
            IEnumerable<OperationDefinitionNode> oprationDefinitions,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (OperationDefinitionNode operation in oprationDefinitions)
            {
                _visited.Clear();
                VisitOperationDefinition(operation, path);
                if (_cycleDetected)
                {
                    return;
                }
            }
        }

        protected override void VisitFragmentDefinitions(
            IEnumerable<FragmentDefinitionNode> fragmentDefinitions,
            ImmutableStack<ISyntaxNode> path)
        {
            // we do not want do visit any fragments separately.
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_cycleDetected)
            {
                return;
            }

            ImmutableStack<ISyntaxNode> newpath = path.Push(fragmentSpread);

            if (path.Last() is DocumentNode d)
            {
                string fragmentName = fragmentSpread.Name.Value;
                if (TryGetFragment(fragmentName,
                    out FragmentDefinitionNode fragment))
                {
                    VisitFragmentDefinition(fragment, newpath);
                }
            }

            VisitDirectives(fragmentSpread.Directives, newpath);
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_cycleDetected)
            {
                return;
            }

            if (fragmentDefinition.TypeCondition?.Name?.Value != null
                && Schema.TryGetType(
                    fragmentDefinition.TypeCondition.Name.Value,
                    out INamedOutputType typeCondition))
            {
                if (_visited.Add(fragmentDefinition))
                {
                    ImmutableStack<ISyntaxNode> newpath = path
                        .Push(fragmentDefinition);

                    VisitSelectionSet(
                        fragmentDefinition.SelectionSet,
                        typeCondition,
                        newpath);

                    VisitDirectives(
                        fragmentDefinition.Directives,
                        newpath);
                }
                else
                {
                    DetectCycle(fragmentDefinition, path);
                }
            }
        }

        private void DetectCycle(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> current = path;

            while (current.Any())
            {
                current = current.Pop(out ISyntaxNode node);

                if (node == fragmentDefinition)
                {
                    _cycleDetected = true;
                    Errors.Add(new ValidationError(
                        "The graph of fragment spreads must not form any " +
                        "cycles including spreading itself. Otherwise an " +
                        "operation could infinitely spread or infinitely " +
                        "execute on cycles in the underlying data.",
                        GetCyclePath(path)));
                    return;
                }
            }
        }

        private IEnumerable<ISyntaxNode> GetCyclePath(
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> current = path;

            while (current.Any())
            {
                current = current.Pop(out ISyntaxNode node);

                if (node is FragmentSpreadNode)
                {
                    yield return node;
                }
            }
        }
    }
}
