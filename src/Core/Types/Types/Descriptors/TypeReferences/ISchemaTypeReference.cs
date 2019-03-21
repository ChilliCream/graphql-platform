namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        ITypeSystemObject Type { get; }
    }
}
