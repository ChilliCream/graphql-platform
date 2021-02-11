namespace StrawberryShake.Serialization
{
    public interface IInputObjectFormatter : IInputValueFormatter
    {
        void Initialize(ISerializerResolver serializerResolver);
    }
}
