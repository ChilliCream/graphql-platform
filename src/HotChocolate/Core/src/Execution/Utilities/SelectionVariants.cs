using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class SelectionVariants : ISelectionVariants
    {
        private ObjectType? _firstType;
        private ISelectionSet? _firstSelections;
        private ObjectType? _secondType;
        private ISelectionSet? _secondSelections;
        private Dictionary<ObjectType, ISelectionSet>? _map;

        public SelectionVariants(SelectionSetNode selectionSet)
        {
            SelectionSet = selectionSet;
        }

        public SelectionSetNode SelectionSet { get; }

        public IEnumerable<ObjectType> GetPossibleTypes()
        {
            if (_map is { })
            {
                foreach (ObjectType possibleType in _map.Keys)
                {
                    yield return possibleType;
                }
            }
            else
            {
                yield return _firstType!;

                if (_secondType is { })
                {
                    yield return _secondType;
                }
            }
        }

        public ISelectionSet GetSelectionSet(ObjectType typeContext)
        {
            if (_map is { })
            {
                return _map.TryGetValue(typeContext, out ISelectionSet? selections)
                    ? selections
                    : Utilities.SelectionSet.Empty;
            }

            if (ReferenceEquals(_firstType, typeContext))
            {
                return _firstSelections!;
            }

            if (ReferenceEquals(_secondType, typeContext))
            {
                return _secondSelections!;
            }

            return Utilities.SelectionSet.Empty;
        }

        public void AddSelectionSet(ObjectType typeContext, ISelectionSet selections)
        {
            if (_map is { })
            {
                _map[typeContext] = selections;
            }
            else
            {
                if (_firstType is null)
                {
                    _firstType = typeContext;
                    _firstSelections = selections;
                }
                else if (_secondType is null)
                {
                    _secondType = typeContext;
                    _secondSelections = selections;
                }
                else
                {
                    _map = new Dictionary<ObjectType, ISelectionSet>
                    {
                        { _firstType, _firstSelections! },
                        { _secondType, _secondSelections! },
                        { typeContext, selections }
                    };

                    _firstType = null;
                    _firstSelections = null;
                    _secondType = null;
                    _secondSelections = null;
                }
            }
        }
    }
}
