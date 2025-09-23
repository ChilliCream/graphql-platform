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
        private int _curIdx;
        private readonly int _endIdxOrVersion;

        internal ArrayEnumerator(SourceResultElement target, int currentIndex = -1)
        {
            _target = target;
            _curIdx = currentIndex;

            Debug.Assert(target.TokenType == JsonTokenType.StartArray);
            _endIdxOrVersion = target._parent.GetEndIndex(_target._index, includeEndElement: false);
        }

        /// <inheritdoc />
        public SourceResultElement Current
        {
            get
            {
                if (_curIdx < 0)
                {
                    return default;
                }

                return new SourceResultElement(_target._parent, _curIdx);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="ArrayEnumerator"/> value that can be used to iterate
        /// through the array.
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
        IEnumerator<SourceResultElement> IEnumerable<SourceResultElement>.GetEnumerator() => GetEnumerator();

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

            return _curIdx < _endIdxOrVersion;
        }
    }
}
