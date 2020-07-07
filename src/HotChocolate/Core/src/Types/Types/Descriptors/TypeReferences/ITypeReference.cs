#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface ITypeReference
    {
        string? Scope { get; }

        bool[]? Nullable { get; }

        TypeContext Context { get; }
    }
}
