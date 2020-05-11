using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedSelection : IPreparedSelection
    {
        private static readonly IReadOnlyList<FieldNode> _emptySelections = new FieldNode[0];
        private List<FieldVisibility>? _visibilities;
        private List<FieldNode>? _selections;
        private bool _isReadOnly = false;

        public PreparedSelection(
            ObjectType declaringType,
            ObjectField field,
            FieldNode selection,
            int responseIndex,
            string responseName,
            FieldDelegate resolverPipeline,
            IReadOnlyDictionary<NameString, PreparedArgument> arguments)
        {
            DeclaringType = declaringType;
            Field = field;
            Selection = selection;
            ResponseIndex = responseIndex;
            ResponseName = responseName;
            ResolverPipeline = resolverPipeline;
            Arguments = arguments;
            Selections = _emptySelections;
        }

        /// <inheritdoc />
        public ObjectType DeclaringType { get; }

        /// <inheritdoc />
        public ObjectField Field { get; }

        /// <inheritdoc />
        public FieldNode Selection { get; private set; }

        public SelectionSetNode? SelectionSet => Selection.SelectionSet;

        /// <inheritdoc />
        public IReadOnlyList<FieldNode> Selections { get; private set; }

        IReadOnlyList<FieldNode> IFieldSelection.Nodes => Selections;

        /// <inheritdoc />
        public int ResponseIndex { get; }

        /// <inheritdoc />
        public NameString ResponseName { get; }

        /// <inheritdoc />
        public FieldDelegate ResolverPipeline { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<NameString, PreparedArgument> Arguments { get; }

        /// <inheritdoc />
        public bool IsFinal { get; private set; } = true;

        IPreparedArgumentMap IPreparedSelection.Arguments => throw new NotImplementedException();

        /// <inheritdoc />
        public bool IsVisible(IVariableValueCollection variables)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            if (_visibilities is null)
            {
                return true;
            }

            if (_visibilities.Count == 1)
            {
                return _visibilities[0].IsVisible(variables);
            }

            for (var i = 0; i < _visibilities.Count; i++)
            {
                if (!_visibilities[i].IsVisible(variables))
                {
                    return false;
                }
            }

            return true;
        }

        public void TryAddVariableVisibility(FieldVisibility visibility)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            _visibilities ??= new List<FieldVisibility>();
            IsFinal = false;

            if (_visibilities.Count == 0)
            {
                _visibilities.Add(visibility);
            }

            for (var i = 0; i < _visibilities.Count; i++)
            {
                if (_visibilities[i].Equals(visibility))
                {
                    return;
                }
            }

            _visibilities.Add(visibility);
        }

        public void AddSelection(FieldNode field)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            if (_selections is null)
            {
                _selections = new List<FieldNode>();
                _selections.Add(Selection);
            }
            _selections.Add(field);
        }

        public void MakeReadOnly()
        {
            _isReadOnly = true;
            Selection = MergeField(Selection, _selections);

            if (_selections is { })
            {
                Selections = _selections;
            }
        }

        private static FieldNode MergeField(
            FieldNode first,
            IReadOnlyList<FieldNode>? selections)
        {
            if (selections is null)
            {
                return first;
            }

            return new FieldNode
            (
                first.Location,
                first.Name,
                first.Alias,
                MergeDirectives(selections),
                first.Arguments,
                MergeSelections(first, selections)
            );
        }

        private static SelectionSetNode? MergeSelections(
            FieldNode first,
            IReadOnlyList<FieldNode> selections)
        {
            if (first.SelectionSet is null)
            {
                return null;
            }

            var children = new List<ISelectionNode>();

            for (int i = 0; i < selections.Count; i++)
            {
                if (selections[i].SelectionSet is { } selectionSet)
                {
                    children.AddRange(selectionSet.Selections);
                }
            }

            return new SelectionSetNode
            (
                selections[0].SelectionSet!.Location,
                children
            );
        }

        private static IReadOnlyList<DirectiveNode> MergeDirectives(
            IReadOnlyList<FieldNode> selections)
        {
            int firstWithDirectives = -1;
            List<DirectiveNode>? merged = null;

            for (int i = 0; i < selections.Count; i++)
            {
                FieldNode selection = selections[i];
                if (selection.Directives.Count > 0)
                {
                    if (firstWithDirectives == -1)
                    {
                        firstWithDirectives = i;
                    }
                    else if (merged is null)
                    {
                        merged = selections[firstWithDirectives].Directives.ToList();
                        merged.AddRange(selection.Directives);
                    }
                    else
                    {
                        merged.AddRange(selection.Directives);
                    }
                }
            }

            if (merged is { })
            {
                return merged;
            }

            if (firstWithDirectives != -1)
            {
                return selections[firstWithDirectives].Directives;
            }

            return selections[0].Directives;
        }
    }
}
