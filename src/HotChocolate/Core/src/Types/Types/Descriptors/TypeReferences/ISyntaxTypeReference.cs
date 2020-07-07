using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface ISyntaxTypeReference
        : ITypeReference
        , IEquatable<ISyntaxTypeReference>
    {
        ITypeNode Type { get; }

        ISyntaxTypeReference WithType(ITypeNode type);

        ISyntaxTypeReference WithContext(TypeContext context = TypeContext.None);

        ISyntaxTypeReference WithScope(string? scope = null);

        ISyntaxTypeReference WithNullable(bool[]? nullable = null);

        ISyntaxTypeReference With(
            Optional<ITypeNode> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default);
    }
}
