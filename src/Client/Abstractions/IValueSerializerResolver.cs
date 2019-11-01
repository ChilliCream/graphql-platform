namespace StrawberryShake
{
    public interface IValueSerializerResolver
    {
        IValueSerializer GetValueSerializer(string typeName);
    }
}
