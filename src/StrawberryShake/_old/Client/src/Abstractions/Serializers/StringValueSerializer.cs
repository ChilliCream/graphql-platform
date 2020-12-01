namespace StrawberryShake.Serializers
{
    public class StringValueSerializer
        : ValueSerializerBase<string, string>
    {
        public override string Name => WellKnownScalars.String;

        public override ValueKind Kind => ValueKind.String;
    }
}
