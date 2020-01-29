namespace StrawberryShake.Serializers
{
    public class ShortValueSerializer
        : IntegerValueSerializerBase<short>
    {
        public override string Name => WellKnownScalars.Short;
    }
}
