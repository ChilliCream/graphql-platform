namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ITypeReference
    {
        bool? IsTypeNullable { get; }

        bool? IsElementTypeNullable { get; }
    }
}
