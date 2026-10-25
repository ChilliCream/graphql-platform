using System.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal readonly partial struct SourceResultElementBuilder
{
    /// <summary>
    /// An enumerator over the property slots of a JSON object under construction.
    /// Each slot is paired with the selection it was created for, so the object's
    /// layout stays owned by the builder alone.
    /// </summary>
    [DebuggerDisplay("{Current,nq}")]
    public struct PropertyEnumerator
    {
        private readonly SourceResultDocumentBuilder _builder;
        private readonly int _startIndex;
        private readonly int _propertyCount;
        private int _current;

        internal PropertyEnumerator(SourceResultElementBuilder objectElement)
        {
            Debug.Assert(objectElement.TokenType is ElementTokenType.StartObject);

            _builder = objectElement._builder;
            _startIndex = _builder.GetStartIndex(objectElement._index);
            _propertyCount = _builder.GetPropertyCount(_startIndex);
            _current = -1;
        }

        public (Selection Selection, SourceResultElementBuilder Value) Current
        {
            get
            {
                if (_current < 0)
                {
                    return default;
                }

                var propertyIndex = _startIndex + (_current * 2) + 1;

                return (
                    _builder.GetPropertySelection(propertyIndex),
                    new SourceResultElementBuilder(_builder, propertyIndex + 1));
            }
        }

        public PropertyEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator._current = -1;
            return enumerator;
        }

        public bool MoveNext()
        {
            if (_current + 1 >= _propertyCount)
            {
                return false;
            }

            _current++;
            return true;
        }
    }
}
