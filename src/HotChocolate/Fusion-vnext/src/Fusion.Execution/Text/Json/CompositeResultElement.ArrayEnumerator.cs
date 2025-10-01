using System.Collections;
using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

public partial struct CompositeResultElement
{
    /// <summary>
    ///   An enumerable and enumerator for the contents of a JSON array.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ArrayEnumerator : IEnumerable<CompositeResultElement>, IEnumerator<CompositeResultElement>
    {
        private readonly CompositeResultElement _target;
        private int _index;
        private readonly int _startIndex;
        private readonly int _endIndex;

        internal ArrayEnumerator(CompositeResultElement target)
        {
            _target = target;
            _index = _startIndex = target._parent.GetStartIndex(_target._index);
            _endIndex = target._parent.GetEndIndex(_index);
        }

        /// <inheritdoc />
        public CompositeResultElement Current
        {
            get
            {
                if (_index == _startIndex || _index >= _endIndex)
                {
                    return default;
                }

                return new CompositeResultElement(_target._parent, _index);
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///   An <see cref="ArrayEnumerator"/> value that can be used to iterate
        ///   through the array.
        /// </returns>
        public ArrayEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._index = enumerator._startIndex;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<CompositeResultElement> IEnumerable<CompositeResultElement>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc />
        public void Dispose()
        {
            _index = _endIndex;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = _startIndex;
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_index >= _endIndex)
            {
                return false;
            }

            _index++;
            return _index < _endIndex;
        }
    }
}
