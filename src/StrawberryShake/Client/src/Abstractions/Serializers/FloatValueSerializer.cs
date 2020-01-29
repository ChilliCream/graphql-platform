namespace StrawberryShake.Serializers
{
    public class FloatValueSerializer
        : FloatValueSerializerBase<double>
    {
        public override string Name => WellKnownScalars.Float;
    }
}
