namespace StrawberryShake.Serializers
{
    public abstract class FloatValueSerializerBase<T>
        : ValueSerializerBase<T, T>
        where T : struct
    {
        public override ValueKind Kind => ValueKind.Float;
    }
}
