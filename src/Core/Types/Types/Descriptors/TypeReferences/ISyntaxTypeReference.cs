using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public interface ISyntaxTypeReference
        : ITypeReference
    {
        ITypeNode Type { get; }

        ISyntaxTypeReference WithType(ITypeNode type);
    }
}
