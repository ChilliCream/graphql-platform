// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct SourceResultElement
{
    /// <summary>
    /// An enumerable and enumerator for the contents of a JSON array.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ArrayEnumerator : IEnumerable<SourceResultElement>, IEnumerator<SourceResultElement>
    {
        private readonly SourceResultElement _target;
        private readonly SourceResultDocument.Cursor _endCursor; // exclusive frontier (start of EndArray)
        private SourceResultDocument.Cursor _current;        // points at the current element start
        private bool _hasStarted;                            // tracks "before start" vs "on an element"

        internal ArrayEnumerator(SourceResultElement target)
        {
            Debug.Assert(target.TokenType == JsonTokenType.StartArray);

            _target = target;
            _endCursor = target._parent.GetEndIndex(target._cursor, includeEndElement: false);

            _current = default;
            _hasStarted = false;
        }

        /// <inheritdoc />
        public SourceResultElement Current
        {
            get
            {
                if (!_hasStarted)
                {
                    return default;
                }

                return new SourceResultElement(_target._parent, _current);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public ArrayEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._hasStarted = false;
            enumerator._current = default;
            return enumerator;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator<SourceResultElement> IEnumerable<SourceResultElement>.GetEnumerator() => GetEnumerator();

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
            // If we haven't started, move to the first element (cursor + 1 row).
            if (!_hasStarted)
            {
                var first = _target._cursor + 1;

                if (first < _endCursor)
                {
                    _current = first;
                    _hasStarted = true;
                    return true;
                }
                else
                {
                    // Empty array: no elements before EndArray.
                    _current = _endCursor;
                    _hasStarted = false;
                    return false;
                }
            }

            // Already on an element: jump to just after this element (exclusive),
            // which is the start of the next element if one exists.
            var next = _target._parent.GetEndIndex(_current, includeEndElement: true);

            if (next < _endCursor)
            {
                _current = next;
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
