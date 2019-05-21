namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
    {
        ITypeSystemMember Type { get; }
    }
}
