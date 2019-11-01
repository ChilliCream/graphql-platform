namespace StrawberryShake.Serializers
{
    public class IntValueSerializer
        : IntegerValueSerializerBase<int>
    {
        public override string Name => WellKnownScalars.Int;
    }
}
