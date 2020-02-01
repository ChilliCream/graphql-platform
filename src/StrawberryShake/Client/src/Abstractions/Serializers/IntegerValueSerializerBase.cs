namespace StrawberryShake.Serializers
{
    public abstract class IntegerValueSerializerBase<T>
        : ValueSerializerBase<T, T>
        where T : struct
    {
        public override ValueKind Kind => ValueKind.Integer;
    }
}
