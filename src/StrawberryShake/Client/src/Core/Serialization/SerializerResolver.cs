using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.Serialization
{
    public class SerializerResolver : ISerializerResolver
    {
        private readonly Dictionary<string, ISerializer> _serializers;

        public SerializerResolver(IEnumerable<ISerializer> serializers)
        {
            if (serializers is null)
            {
                throw new ArgumentNullException(nameof(serializers));
            }

            _serializers = serializers.ToDictionary(t => t.TypeName);
        }

        public ILeafValueParser<TSerialized, TRuntime> GetLeafValueParser<TSerialized, TRuntime>(
            string typeName)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_serializers.TryGetValue(typeName, out ISerializer? serializer) &&
                serializer is ILeafValueParser<TSerialized, TRuntime> parser)
            {
                return parser;
            }

            throw new ArgumentException("There is no parser registered the specified type.");
        }

        public IInputValueFormatter GetInputValueFormatter(string typeName)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_serializers.TryGetValue(typeName, out ISerializer? serializer) &&
                serializer is IInputValueFormatter formatter)
            {
                return formatter;
            }

            throw new ArgumentException("There is no formatter registered the specified type.");
        }
    }
}
