namespace StrawberryShake.Serializers
{
    public class LongValueSerializer
        : IntegerValueSerializerBase<long>
    {
        public override string Name => WellKnownScalars.Long;
    }
}
