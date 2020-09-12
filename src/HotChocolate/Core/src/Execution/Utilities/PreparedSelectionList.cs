using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution.Utilities
{
    internal class PreparedSelectionList : IPreparedSelectionList
    {
        private readonly IReadOnlyList<IPreparedSelection> _selections;

        public PreparedSelectionList(
            IReadOnlyList<IPreparedSelection> selections,
            bool isConditional)
        {
            _selections = selections;
            IsConditional = isConditional;
        }

        public IPreparedSelection this[int index] => _selections[index];

        public bool IsConditional { get; }

        public int Count => _selections.Count;

        public IEnumerator<IPreparedSelection> GetEnumerator() => _selections.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static PreparedSelectionList Empty { get; } =
            new PreparedSelectionList(new IPreparedSelection[0], false);
    }
}
