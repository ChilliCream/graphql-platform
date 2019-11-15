using System.Collections.Generic;
namespace StrawberryShake.Serializers
{
    public static class ValueSerializers
    {
        public static IReadOnlyList<IValueSerializer> All { get; } =
            new IValueSerializer[]
            {
                new BooleanValueSerializer(),
                new ByteValueSerializer(),
                new DateTimeValueSerializer(),
                new DateValueSerializer(),
                new DecimalValueSerializer(),
                new FloatValueSerializer(),
                new IdValueSerializer(),
                new IntValueSerializer(),
                new LongValueSerializer(),
                new ShortValueSerializer(),
                new StringValueSerializer(),
                new UrlValueSerializer(),
                new UuidValueSerializer(),
                new UuidValueSerializer(WellKnownScalars.Guid),
            };
    }
}
