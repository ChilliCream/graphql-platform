using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ITypeSyntaxReference
        : ITypeReference
    {
        ITypeNode Type { get; }
    }
}
