using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution.Utilities
{
    internal class PreparedSelectionList : IPreparedSelectionList
    {
        private readonly IReadOnlyList<IPreparedSelection> _selections;

        public PreparedSelectionList(IReadOnlyList<IPreparedSelection> selections, bool isFinal)
        {
            _selections = selections;
            IsFinal = isFinal;
        }

        public IPreparedSelection this[int index] => _selections[index];

        public bool IsFinal {get; }

        public int Count => _selections.Count;

        public IEnumerator<IPreparedSelection> GetEnumerator() => _selections.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
