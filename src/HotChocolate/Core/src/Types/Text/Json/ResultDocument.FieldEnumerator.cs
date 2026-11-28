using System.Diagnostics;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    /// <summary>
    /// Enumerates the fields of an object that was created for a selection set:
    /// each selection that is not excluded, together with the element that holds its value.
    /// </summary>
    internal ref struct FieldEnumerator
    {
        private readonly ResultDocument _document;
        private readonly ReadOnlySpan<Selection> _selections;
        private Cursor _cursor;
        private int _index;

        /// <summary>
        /// Initializes an enumerator that is positioned before the first field of the object at
        /// <paramref name="start"/>, which was created for <paramref name="selectionSet"/>.
        /// </summary>
        internal FieldEnumerator(ResultDocument document, Cursor start, SelectionSet selectionSet)
        {
            Debug.Assert(document._metaDb.GetElementTokenType(start) is ElementTokenType.StartObject);
            Debug.Assert(document._metaDb.GetSizeOrLength(start) == selectionSet.Selections.Length);

            _document = document;
            _selections = selectionSet.Selections;
            _cursor = start;
            _index = -1;
        }

        /// <summary>
        /// Gets the selection of the current field.
        /// </summary>
        public readonly Selection Selection => _selections[_index];

        /// <summary>
        /// Gets the element that holds the value of the current field.
        /// </summary>
        public readonly ResultElement Value => new(_document, _cursor.AddRows(1));

        /// <summary>
        /// Advances to the next field whose selection is not excluded.
        /// </summary>
        public bool MoveNext()
        {
            var selections = _selections;
            var index = _index + 1;

            while ((uint)index < (uint)selections.Length)
            {
                // The object holds one property row and one value row per selection, in selection order.
                _cursor = _cursor.AddRows(index == 0 ? 1 : 2);

                Debug.Assert(_document._metaDb.Get(_cursor).TokenType is ElementTokenType.PropertyName);
                Debug.Assert(_document._metaDb.Get(_cursor).OperationReferenceId == selections[index].Id);

                if ((_document._metaDb.GetFlags(_cursor) & ElementFlags.IsExcluded) != ElementFlags.IsExcluded)
                {
                    _index = index;
                    return true;
                }

                index++;
            }

            _index = selections.Length;
            return false;
        }
    }
}
