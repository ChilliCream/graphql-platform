namespace StrawberryShake.Serializers
{
    public class ByteValueSerializer
        : IntegerValueSerializerBase<byte>
    {
        public override string Name => WellKnownScalars.Byte;
    }
}
