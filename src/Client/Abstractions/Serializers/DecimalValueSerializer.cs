using System;

namespace StrawberryShake.Serializers
{
    public class DecimalValueSerializer
        : FloatValueSerializerBase<Decimal>
    {
        public override string Name => WellKnownScalars.Decimal;
    }
}
