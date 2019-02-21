using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public interface ITypeSyntaxReference
        : ITypeReference
    {
        ITypeNode Type { get; }
    }
}
