using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal interface ITypeRegistrar
{
    void Register(
        TypeSystemObjectBase obj,
        string? scope,
        bool inferred = false,
        Action<RegisteredType>? configure = null);

    void MarkUnresolved(ITypeReference typeReference);

    void MarkResolved(ITypeReference typeReference);

    bool IsResolved(ITypeReference typeReference);

    TypeSystemObjectBase CreateInstance(Type namedSchemaType);

    IReadOnlyCollection<ITypeReference> Unresolved { get; }

    IReadOnlyCollection<ITypeReference> GetUnhandled();
}
