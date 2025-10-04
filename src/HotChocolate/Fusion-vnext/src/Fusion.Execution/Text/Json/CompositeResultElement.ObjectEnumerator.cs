// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct CompositeResultElement
{
    /// <summary>
    ///   An enumerable and enumerator for the properties of a JSON object.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct ObjectEnumerator : IEnumerable<CompositeResultProperty>, IEnumerator<CompositeResultProperty>
    {
        private readonly CompositeResultElement _target;
        private int _index;
        private readonly int _startIndex;
        private readonly int _endIndex;

        internal ObjectEnumerator(CompositeResultElement target)
        {
            _target = target;
            _index = _startIndex = target._parent.GetStartIndex(_target._index);
            _endIndex = target._parent.GetEndIndex(_index);
        }

        /// <inheritdoc />
        public CompositeResultProperty Current
        {
            get
            {
                if (_index == _startIndex || _index >= _endIndex)
                {
                    return default;
                }

                return new CompositeResultProperty(new CompositeResultElement(_target._parent, _index));
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates the properties of an object.
        /// </summary>
        /// <returns>
        ///   An <see cref="ObjectEnumerator"/> value that can be used to iterate
        ///   through the object.
        /// </returns>
        /// <remarks>
        ///   The enumerator will enumerate the properties in the order they are
        ///   declared, and when an object has multiple definitions of a single
        ///   property they will all individually be returned (each in the order
        ///   they appear in the content).
        /// </remarks>
        public ObjectEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._index = enumerator._startIndex;
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

            _index += 2;
            return _index < _endIndex;
        }
    }
}
