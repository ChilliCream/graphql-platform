using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedSelection
        : IPreparedSelection
        , IFieldSelection
    {
        private List<FieldVisibility>? _visibilities;

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
            Selections.Add(selection);
        }

        /// <inheritdoc />
        public ObjectType DeclaringType { get; }

        /// <inheritdoc />
        public ObjectField Field { get; }

        /// <inheritdoc />
        public FieldNode Selection { get; }

        /// <inheritdoc />
        public List<FieldNode> Selections { get; } = new List<FieldNode>();

        IReadOnlyList<FieldNode> IPreparedSelection.Selections => Selections;

        IReadOnlyList<FieldNode> IFieldSelection.Selections => Selections;

        IReadOnlyList<FieldNode> IFieldSelection.Nodes => Selections;

        /// <inheritdoc />
        public int ResponseIndex { get; }

        /// <inheritdoc />
        public string ResponseName { get; }

        /// <inheritdoc />
        public FieldDelegate ResolverPipeline { get; set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<NameString, PreparedArgument> Arguments { get; }

        /// <inheritdoc />
        public bool IsVisible(IVariableValueCollection variables)
        {
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
            _visibilities ??= new List<FieldVisibility>();

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
    }
}
