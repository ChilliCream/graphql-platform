using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public sealed class ValueSerializerResolver
        : IValueSerializerResolver
    {
        private readonly IReadOnlyDictionary<string, IValueSerializer> _serializers;

        public ValueSerializerResolver(IEnumerable<IValueSerializer> valueSerializers)
        {
            if (valueSerializers is null)
            {
                throw new ArgumentNullException(nameof(valueSerializers));
            }

            _serializers = valueSerializers.ToDictionary();

            foreach (var serializer in _serializers.Values.OfType<IInputSerializer>())
            {
                serializer.Initialize(this);
            }
        }

        public IValueSerializer GetValueSerializer(string typeName)
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
    }
}
