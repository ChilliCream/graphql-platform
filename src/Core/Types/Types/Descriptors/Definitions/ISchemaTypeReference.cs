namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        IType Type { get; }
    }
}
