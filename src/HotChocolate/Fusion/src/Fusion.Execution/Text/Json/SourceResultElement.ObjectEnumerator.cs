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
        private readonly SourceResultDocument.Cursor _endCursor;     // exclusive frontier (Start of EndObject)
        private SourceResultDocument.Cursor _current;                // points at the current property's VALUE
        private bool _hasStarted;                                    // before-start sentinel

        internal ObjectEnumerator(SourceResultElement target)
        {
            Debug.Assert(target.TokenType == JsonTokenType.StartObject);

            _target = target;
            _endCursor = target._parent.GetEndIndex(target._cursor, includeEndElement: false);

            _current = default;
            _hasStarted = false;
        }

        /// <inheritdoc />
        public SourceResultProperty Current
        {
            get
            {
                if (!_hasStarted)
                {
                    return default;
                }

                return new SourceResultProperty(new SourceResultElement(_target._parent, _current));
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
            enumerator._hasStarted = false;
            enumerator._current = default;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<SourceResultProperty> IEnumerable<SourceResultProperty>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void Dispose()
        {
            _hasStarted = false;
            _current = _endCursor;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _hasStarted = false;
            _current = default;
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (!_hasStarted)
            {
                // First property: after StartObject comes PropertyName (+1), then Value (+1).
                var firstName = _target._cursor + 1;
                var firstValue = firstName + 1;

                if (firstValue < _endCursor)
                {
                    _current = firstValue;
                    _hasStarted = true;
                    return true;
                }
                else
                {
                    // Empty object ({}): no properties before EndObject.
                    _current = _endCursor;
                    _hasStarted = false;
                    return false;
                }
            }

            // Advance past the current VALUE to the start of the next element.
            // GetEndIndex(current, includeEndElement: true) yields the row after the current value.
            var afterCurrent = _target._parent.GetEndIndex(_current, includeEndElement: true);

            // After a value, the next row (if any) is the next PropertyName; we need the VALUE,
            // so we skip one more row.
            var nextValue = afterCurrent + 1;

            if (nextValue < _endCursor)
            {
                _current = nextValue;
                return true;
            }
            else
            {
                _current = _endCursor;
                _hasStarted = false;
                return false;
            }
        }
    }
}
