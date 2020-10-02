using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class SelectionVariants : ISelectionVariants
    {
        private IObjectType? _firstType;
        private ISelectionSet? _firstSelections;
        private IObjectType? _secondType;
        private ISelectionSet? _secondSelections;
        private Dictionary<IObjectType, ISelectionSet>? _map;

        public SelectionVariants(SelectionSetNode selectionSet)
        {
            SelectionSet = selectionSet;
        }

        public SelectionSetNode SelectionSet { get; }

        public IEnumerable<IObjectType> GetPossibleTypes()
        {
            if (_map is { })
            {
                foreach (IObjectType possibleType in _map.Keys)
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

        public ISelectionSet GetSelectionSet(IObjectType typeContext)
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

        public void AddSelectionSet(
            IObjectType typeContext, 
            IReadOnlyList<ISelection> selections,
            IReadOnlyList<IFragment>? fragments,
            bool isConditional)
        {
            var selectionSet = new SelectionSet(selections, fragments, isConditional);

            if (_map is { })
            {
                _map[typeContext] = selectionSet;
            }
            else
            {
                if (_firstType is null)
                {
                    _firstType = typeContext;
                    _firstSelections = selectionSet;
                }
                else if (_secondType is null)
                {
                    _secondType = typeContext;
                    _secondSelections = selectionSet;
                }
                else
                {
                    _map = new Dictionary<IObjectType, ISelectionSet>
                    {
                        { _firstType, _firstSelections! },
                        { _secondType, _secondSelections! },
                        { typeContext, selectionSet }
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
