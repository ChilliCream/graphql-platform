using System.Collections;
using System.Diagnostics;
using static HotChocolate.Fusion.Text.Json.CompositeResultDocument;

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct CompositeResultElement
{
    /// <summary>
    /// An enumerable and enumerator for the properties of a JSON object.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ObjectEnumerator : IEnumerable<CompositeResultProperty>, IEnumerator<CompositeResultProperty>
    {
        private readonly CompositeResultDocument _document;
        private readonly Cursor _start;
        private readonly Cursor _end;
        private Cursor _cursor;

        internal ObjectEnumerator(CompositeResultElement target)
        {
            _document = target._parent;
            (_start, var tokenType) = _document._metaDb.GetStartCursor(target._cursor);
            Debug.Assert(tokenType is ElementTokenType.StartObject);
            _end = _start + _document._metaDb.GetNumberOfRows(_start);
            _cursor = _start;
        }

        /// <inheritdoc />
        public CompositeResultProperty Current
        {
            get
            {
                if (_cursor == _start || _cursor >= _end)
                {
                    return default;
                }

                return new CompositeResultProperty(new CompositeResultElement(_document, _cursor + 1));
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates the properties of an object.
        /// </summary>
        /// <returns>
        /// An <see cref="ObjectEnumerator"/> value that can be used to iterate
        /// through the object.
        /// </returns>
        /// <remarks>
        /// The enumerator will enumerate the properties in the order they are
        /// declared, and when an object has multiple definitions of a single
        /// property they will all individually be returned (each in the order
        /// they appear in the content).
        /// </remarks>
        public ObjectEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._cursor = enumerator._start;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<CompositeResultProperty> IEnumerable<CompositeResultProperty>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        public void Dispose()
        {
            _cursor = _end;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _cursor = _start;
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            while (MoveNextInternal())
            {
                var flags = _document._metaDb.GetFlags(_cursor);
                if ((ElementFlags.IsExcluded & flags) is not ElementFlags.IsExcluded)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveNextInternal()
        {
            if (_cursor == _start)
            {
                var first = _start + 1;
                if (first < _end)
                {
                    _cursor = first;
                    return true;
                }

                _cursor = _end;
                return false;
            }

            var next = _cursor += 2;
            if (next < _end)
            {
                _cursor = next;
                return true;
            }

            _cursor = _end;
            return false;
        }
    }
}
