using System.Collections;
using System.Diagnostics;

namespace HotChocolate.Text.Json;

public readonly partial struct ResultElement
{
    /// <summary>
    /// An enumerable and enumerator for the contents of a JSON array.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ArrayEnumerator : IEnumerable<ResultElement>, IEnumerator<ResultElement>
    {
        private readonly ResultDocument _document;
        private readonly ResultDocument.Cursor _start;
        private readonly ResultDocument.Cursor _end;
        private ResultDocument.Cursor _cursor;

        internal ArrayEnumerator(ResultElement target)
        {
            _document = target._parent;
            (_start, var tokenType) = _document._metaDb.GetStartCursor(target._cursor);
            Debug.Assert(tokenType is ElementTokenType.StartArray);
            _end = _start + _document._metaDb.GetNumberOfRows(_start);
            _cursor = _start;
        }

        /// <inheritdoc />
        public ResultElement Current
        {
            get
            {
                var cursor = _cursor;
                if (cursor == _start || cursor >= _end)
                {
                    return default;
                }

                return new ResultElement(_document, cursor);
            }
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <summary>
        ///   Returns an enumerator that iterates through the array.
        /// </summary>
        public ArrayEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._cursor = enumerator._start;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<ResultElement> IEnumerable<ResultElement>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        public bool MoveNext()
        {
            var start = _start;
            var end = _end;

            if (_cursor == start)
            {
                var first = start + 1;
                if (first < end)
                {
                    _cursor = first;
                    return true;
                }

                _cursor = end;
                return false;
            }

            var next = _cursor + 1;
            if (next < end)
            {
                _cursor = next;
                return true;
            }

            _cursor = end;
            return false;
        }

        /// <inheritdoc />
        public void Reset() => _cursor = _start;

        /// <inheritdoc />
        public void Dispose() => _cursor = _end;
    }
}
