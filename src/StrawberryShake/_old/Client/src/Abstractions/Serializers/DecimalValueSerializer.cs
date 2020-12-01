using System;

namespace StrawberryShake.Serializers
{
    public class DecimalValueSerializer
        : FloatValueSerializerBase<decimal>
    {
        public override string Name => WellKnownScalars.Decimal;
    }
}
