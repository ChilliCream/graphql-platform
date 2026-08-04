using System.Collections;
using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

internal readonly partial struct SourceResultElementBuilder
{
    /// <summary>
    /// An enumerable and enumerator for the contents of a JSON array.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ArrayEnumerator : IEnumerable<SourceResultElementBuilder>, IEnumerator<SourceResultElementBuilder>
    {
        private readonly SourceResultDocumentBuilder _builder;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private int _current;

        internal ArrayEnumerator(SourceResultElementBuilder arrayElement)
        {
            Debug.Assert(arrayElement.TokenType == ElementTokenType.StartArray);

            _builder = arrayElement._builder;
            _startIndex = _builder.GetStartIndex(arrayElement._index);
            _endIndex = _builder.GetEndIndex(_startIndex);
            _current = -1;
        }

        /// <inheritdoc />
        public SourceResultElementBuilder Current
        {
            get
            {
                if (_current == -1)
                {
                    return default;
                }

                return new SourceResultElementBuilder(_builder, _current);
            }
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public ArrayEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._current = -1;
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        IEnumerator<SourceResultElementBuilder> IEnumerable<SourceResultElementBuilder>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        public void Dispose()
            => _current = _endIndex;

        /// <inheritdoc />
        public void Reset()
            => _current = -1;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_current == -1)
            {
                var first = _startIndex + 1;
                if (first < _endIndex)
                {
                    _current = first;
                    return true;
                }

                _current = _endIndex;
                return false;
            }

            var next = _current + 1;
            if (next < _endIndex)
            {
                _current = next;
                return true;
            }

            _current = _endIndex;
            return false;
        }
    }
}
