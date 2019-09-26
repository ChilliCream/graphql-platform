using System;

namespace StrawberryShake.Serializers
{
    public class DateValueSerializer
        : ValueSerializerBase<DateTime, string>
    {
        public override string Name => WellKnownScalars.Date;

        public override ValueKind Kind => ValueKind.String;
    }
}
