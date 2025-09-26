using System.Collections;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct SourceResultElement
{
    /// <summary>
    /// An enumerable and enumerator for the properties of a JSON object.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ObjectEnumerator : IEnumerable<SourceResultProperty>, IEnumerator<SourceResultProperty>
    {
        private readonly SourceResultElement _target;
        private int _curIdx;
        private readonly int _endIdxOrVersion;

        internal ObjectEnumerator(SourceResultElement target, int currentIndex = -1)
        {
            _target = target;
            _curIdx = currentIndex;

            Debug.Assert(target.TokenType == JsonTokenType.StartObject);
            _endIdxOrVersion = target._parent.GetEndIndex(_target._index, includeEndElement: false);
        }

        /// <inheritdoc />
        public SourceResultProperty Current
        {
            get
            {
                if (_curIdx < 0)
                {
                    return default;
                }

                return new SourceResultProperty(new SourceResultElement(_target._parent, _curIdx));
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
            enumerator._curIdx = -1;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<SourceResultProperty> IEnumerable<SourceResultProperty>.GetEnumerator() => GetEnumerator();

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
                _curIdx = _target._index + SourceResultDocument.DbRow.Size;
            }
            else
            {
                _curIdx = _target._parent.GetEndIndex(_curIdx, includeEndElement: true);
            }

            // _curIdx is now pointing at a property name, move one more to get the value
            _curIdx += SourceResultDocument.DbRow.Size;

            return _curIdx < _endIdxOrVersion;
        }
    }
}
