using System;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class ReviewInputSerializer
         : IValueSerializer
    {
        private readonly IValueSerializer_stringSerializer;
        private readonly IValueSerializer_intSerializer;
        public ReviewInputSerializer(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer serializer)){
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;

            if (!map.TryGetValue("Int", out IValueSerializer serializer)){
                throw new ArgumentException(
                    "There is no serializer specified for `Int`.",
                    nameof(serializers));
            }
            _intSerializer = serializer;
        }
        public string Name { get } ="ReviewInput";

        public ValueKind Kind { get } = ValueKind.InputObject;

        public ValueKind Kind { get } = typeof(ReviewInput);

        public object Serialize(object value)
        {
            if(value is null)
            {
                return null;
            }
            var input = (ReviewInput)value;var map = new Dictionary<string, object>();
            map["commentary"] = _stringSerializer.Serialize(input.commentary);
            map["stars"] = _intSerializer.Serialize(input.stars);
        }
}
}
