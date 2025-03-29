using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal interface ITypeRegistrar
{
    ISet<string> Scalars { get; }

    void Register(
        TypeSystemObjectBase obj,
        string? scope,
        bool inferred = false,
        Action<RegisteredType>? configure = null);

    void MarkUnresolved(TypeReference typeReference);

    void MarkResolved(TypeReference typeReference);

    bool IsResolved(TypeReference typeReference);

    TypeSystemObjectBase CreateInstance(Type namedSchemaType);

    IReadOnlyCollection<TypeReference> Unresolved { get; }

    IReadOnlyCollection<TypeReference> GetUnhandled();
}
