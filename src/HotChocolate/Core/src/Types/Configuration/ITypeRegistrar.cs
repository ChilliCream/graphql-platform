using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal interface ITypeRegistrar
{
    ISet<string> Scalars { get; }

    void Register(
        TypeSystemObject obj,
        string? scope,
        bool inferred = false,
        Action<RegisteredType>? configure = null);

    void MarkUnresolved(TypeReference typeReference);

    void MarkResolved(TypeReference typeReference);

    bool IsResolved(TypeReference typeReference);

    TypeSystemObject CreateInstance(Type namedSchemaType);

    IReadOnlyCollection<TypeReference> Unresolved { get; }

    IReadOnlyCollection<TypeReference> GetUnhandled();
}
