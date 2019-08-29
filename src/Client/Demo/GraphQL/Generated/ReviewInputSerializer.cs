using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class ReviewInputSerializer
        : IValueSerializer
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _intSerializer;

        public ReviewInputSerializer(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;

            if (!map.TryGetValue("Int", out serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `Int`.",
                    nameof(serializers));
            }
            _intSerializer = serializer;
        }

        public string Name { get; } ="ReviewInput";

        public ValueKind Kind { get; } = ValueKind.InputObject;

        public Type ClrType => typeof(ReviewInput);
        public Type SerializationType => 
        typeof(IReadOnlyDictionary<string, object>);


        public object Serialize(object value)
        {
            if(value is null)
            {
                return null;
            }

            var input = (ReviewInput)value;

            var map = new Dictionary<string, object>();
            map["commentary"] = _stringSerializer.Serialize(input.Commentary);
            map["stars"] = _intSerializer.Serialize(input.Stars);
            return map;
        }

        public object Deserialize(object value)
        {
            throw new NotSupportedException(
                "Deserializing input values is not supported.");
        }
    }
}
