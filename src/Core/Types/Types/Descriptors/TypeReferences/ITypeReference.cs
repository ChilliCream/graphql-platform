namespace HotChocolate.Types.Descriptors
{
    public interface ITypeReference
    {
        bool? IsTypeNullable { get; }

        bool? IsElementTypeNullable { get; }

        TypeContext Context { get; }
    }
}
