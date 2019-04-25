namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        ITypeSystem Type { get; }
    }
}
