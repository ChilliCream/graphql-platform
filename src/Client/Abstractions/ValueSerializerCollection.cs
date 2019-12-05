using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public sealed class ValueSerializerCollection
        : IValueSerializerCollection
    {
        private readonly IReadOnlyDictionary<string, IValueSerializer> _serializers;

        public ValueSerializerCollection(
            IReadOnlyDictionary<string, IValueSerializer> serializers)
        {
            _serializers = serializers ?? throw new ArgumentNullException(nameof(serializers));

            foreach (var serializer in _serializers.Values.OfType<IInputSerializer>())
            {
                serializer.Initialize(this);
            }

            Count = serializers.Count;
        }

        public int Count { get; }

        public IValueSerializer Get(string typeName)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (!_serializers.TryGetValue(typeName, out IValueSerializer? serializer))
            {
                throw new ArgumentException(
                    $"The type `{typeName}` has no value serializer " +
                    "and cannot be handled.");
            }

            return serializer;
        }

        public IEnumerator<IValueSerializer> GetEnumerator()
        {
            return _serializers.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
