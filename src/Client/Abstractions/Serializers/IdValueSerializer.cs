namespace StrawberryShake.Serializers
{
    public class IdValueSerializer
        : ValueSerializerBase<string, string>
    {
        public override string Name => WellKnownScalars.ID;

        public override ValueKind Kind => ValueKind.String;
    }
}
