// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json
{
    public partial struct CompositeResultElement
    {
        /// <summary>
        ///   An enumerable and enumerator for the properties of a JSON object.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ObjectEnumerator : IEnumerable<CompositeResultProperty>, IEnumerator<CompositeResultProperty>
        {
            private readonly CompositeResultElement _target;
            private int _index;
            private readonly int _endIndex;

            internal ObjectEnumerator(CompositeResultElement target, int currentIndex = -1)
            {
                _target = target;
                _index = currentIndex;

                Debug.Assert(target.TokenType == ElementTokenType.StartObject);
                _endIndex = target._parent.GetEndIndex(_target._index);
            }

            /// <inheritdoc />
            public CompositeResultProperty Current
            {
                get
                {
                    if (_index < 0)
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
                ObjectEnumerator ator = this;
                ator._index = -1;
                return ator;
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
                _index = -1;
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

                if (_index < 0)
                {
                    _index = _target._index + 1;
                }
                else
                {
                    _index = _target._parent.GetEndIndex(_index);
                }

                // _curIdx is now pointing at a property name, move one more to get the value
                _index++;

                return _index < _endIndex;
            }
        }
    }
}
