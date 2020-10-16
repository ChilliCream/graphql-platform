using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public sealed partial class OperationCompiler
    {
        private class CompilerContext
        {
            private readonly HashSet<(SelectionSetNode, NameString)> _processed;
            private readonly Stack<CompilerContext> _backlog;
            private readonly IDictionary<SelectionSetNode, SelectionVariants> _variantsLookup;
            private List<IFragment>? _fragments;

            private CompilerContext(
                Stack<CompilerContext> backlog,
                IObjectType type,
                SelectionSetNode selectionSet,
                SelectionVariants selectionVariants,
                IImmutableList<ISelectionOptimizer> optimizers,
                IDictionary<SelectionSetNode, SelectionVariants> selectionVariantsLookup)
            {
                Type = type;
                Path = ImmutableStack<IObjectField>.Empty;
                Fields = new Dictionary<string, Selection>();
                SelectionSet = selectionSet;
                SelectionVariants = selectionVariants;
                IsInternalSelection = false;
                IncludeConditionLookup =
                    new Dictionary<ISelectionNode, SelectionIncludeCondition>();
                Optimizers = optimizers;
                IsConditional = false;
                _processed = new HashSet<(SelectionSetNode, NameString)>();
                _backlog = backlog;
                _variantsLookup = selectionVariantsLookup;
            }

            private CompilerContext(
                IObjectType type,
                IImmutableStack<IObjectField> path,
                SelectionSetNode selectionSet,
                SelectionVariants selectionVariants,
                bool isInternalSelection,
                IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
                IImmutableList<ISelectionOptimizer> optimizers,
                Stack<CompilerContext> backlog,
                IDictionary<SelectionSetNode, SelectionVariants> selectionVariantsLookup,
                HashSet<(SelectionSetNode, NameString)> processed)
            {
                Type = type;
                Path = path;
                Fields = new Dictionary<string, Selection>();
                SelectionSet = selectionSet;
                SelectionVariants = selectionVariants;
                IsInternalSelection = isInternalSelection;
                IncludeConditionLookup = includeConditionLookup;
                Optimizers = optimizers;
                IsConditional = false;
                _backlog = backlog;
                _variantsLookup = selectionVariantsLookup;
                _processed = processed;
            }

            public IObjectType Type { get; }

            public IImmutableStack<IObjectField> Path { get; }

            public IDictionary<string, Selection> Fields { get; }

            public SelectionSetNode SelectionSet { get; }

            public SelectionVariants SelectionVariants { get; }

            public List<ISelection> Selections { get; } = new List<ISelection>();

            public bool IsInternalSelection { get; }

            public IDictionary<ISelectionNode, SelectionIncludeCondition> IncludeConditionLookup 
            { get; }

            public IImmutableList<ISelectionOptimizer> Optimizers { get; }

            public bool IsConditional { get; set; }

            public ISelectionSet GetSelectionSet() => SelectionVariants.GetSelectionSet(Type);

            public void RegisterFragment(IFragment fragment)
            {
                if (_fragments is null)
                {
                    _fragments = new List<IFragment>();
                }
                _fragments.Add(fragment);
            }

            public void Complete()
            {
                SelectionVariants.AddSelectionSet(Type, Selections, _fragments, IsConditional);
            }

            public void TryBranch(ObjectType type, ISelection selection)
            {
                SelectionSetNode selectionSet = selection.SelectionSet!;

                if(!_processed.Add((selectionSet, type.Name)))
                {
                    return;
                }

                if (!_variantsLookup.TryGetValue(
                    selectionSet,
                    out SelectionVariants? selectionVariants))
                {
                    selectionVariants = new SelectionVariants(selectionSet);
                    _variantsLookup[selectionSet] = selectionVariants;
                }

                var context = new CompilerContext
                (
                    type,
                    Path.Push(selection.Field),
                    selectionSet,
                    selectionVariants,
                    selection.IsInternal,
                    IncludeConditionLookup,
                    RegisterOptimizers(Optimizers, selection.Field),
                    _backlog,
                    _variantsLookup,
                    _processed
                );

                _backlog.Push(context);
            }

            public CompilerContext Branch(FragmentInfo fragment)
            {
                if (!_variantsLookup.TryGetValue(
                    fragment.SelectionSet,
                    out SelectionVariants? selectionVariants))
                {
                    selectionVariants = new SelectionVariants(fragment.SelectionSet);
                    _variantsLookup[fragment.SelectionSet] = selectionVariants;
                }

                var context = new CompilerContext
                (
                    Type,
                    Path,
                    fragment.SelectionSet,
                    selectionVariants,
                    IsInternalSelection,
                    IncludeConditionLookup,
                    Optimizers,
                    _backlog,
                    _variantsLookup,
                    _processed
                );

                return context;
            }

            public static CompilerContext New(
                Stack<CompilerContext> backlog,
                IObjectType type,
                SelectionSetNode selectionSet,
                IImmutableList<ISelectionOptimizer> optimizers,
                IDictionary<SelectionSetNode, SelectionVariants> selectionVariantsLookup)
            {
                var rootSelections = new SelectionVariants(selectionSet);
                selectionVariantsLookup[selectionSet] = rootSelections;

                var context = new CompilerContext
                (
                    backlog,
                    type,
                    selectionSet,
                    rootSelections,
                    optimizers,
                    selectionVariantsLookup
                );

                backlog.Push(context);

                return context;
            }

            private static IImmutableList<ISelectionOptimizer> RegisterOptimizers(
                IImmutableList<ISelectionOptimizer> optimizers,
                IObjectField field)
            {
                if (SelectionOptimizerHelper.TryGetOptimizers(
                    field.ContextData,
                    out IReadOnlyList<ISelectionOptimizer>? fieldOptimizers))
                {
                    foreach (ISelectionOptimizer optimizer in fieldOptimizers)
                    {
                        if (!optimizers.Contains(optimizer))
                        {
                            optimizers = optimizers.Add(optimizer);
                        }
                    }
                }

                return optimizers;
            }
        }
    }
}
