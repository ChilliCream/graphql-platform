namespace StrawberryShake.Serializers
{
    public class BooleanValueSerializer
        : ValueSerializerBase<bool, bool>
    {
        public override string Name => WellKnownScalars.Boolean;

        public override ValueKind Kind => ValueKind.Boolean;
    }
}
