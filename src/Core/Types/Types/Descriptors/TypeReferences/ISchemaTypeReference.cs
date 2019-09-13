namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        ITypeSystemMember Type { get; }

        ISchemaTypeReference WithType(ITypeSystemMember type);
    }
}
