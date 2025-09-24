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
        private int _curIdx;
        private readonly int _endIdxOrVersion;

        internal ArrayEnumerator(CompositeResultElement target, int currentIndex = -1)
        {
            _target = target;
            _curIdx = currentIndex;

            Debug.Assert(target.TokenType == ElementTokenType.StartArray);

            _endIdxOrVersion = target._parent.GetEndIndex(_target._index);
        }

        /// <inheritdoc />
        public CompositeResultElement Current
        {
            get
            {
                if (_curIdx < 0)
                {
                    return default;
                }

                return new CompositeResultElement(_target._parent, _curIdx);
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
            enumerator._curIdx = -1;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<CompositeResultElement> IEnumerable<CompositeResultElement>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void Dispose()
        {
            _curIdx = _endIdxOrVersion;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _curIdx = -1;
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_curIdx >= _endIdxOrVersion)
            {
                return false;
            }

            if (_curIdx < 0)
            {
                _curIdx = _target._index + 1;
            }
            else
            {
                _curIdx = _target._parent.GetEndIndex(_curIdx);
            }

            return _curIdx < _endIdxOrVersion;
        }
    }
}
