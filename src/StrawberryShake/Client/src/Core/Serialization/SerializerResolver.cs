using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.Serialization
{
    public class SerializerResolver : ISerializerResolver
    {
        private readonly Dictionary<string, ISerializer> _serializers = new();

        public SerializerResolver(IEnumerable<ISerializer> serializers)
        {
            if (serializers is null)
            {
                throw new ArgumentNullException(nameof(serializers));
            }

            foreach (ISerializer serializer in serializers)
            {
                _serializers[serializer.TypeName] = serializer;
            }

            foreach (IInputObjectFormatter serializer in
                _serializers.Values.OfType<IInputObjectFormatter>())
            {
                serializer.Initialize(this);
            }
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
