namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        IType Type { get; }
    }
}
