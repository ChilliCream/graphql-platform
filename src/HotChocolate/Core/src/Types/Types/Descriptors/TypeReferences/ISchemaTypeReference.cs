using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface ISchemaTypeReference
        : ITypeReference
        , IEquatable<ITypeReference>
    {
        ITypeSystemMember Type { get; }

        ISchemaTypeReference WithType(ITypeSystemMember type);

        ISchemaTypeReference WithContext(TypeContext context = TypeContext.None);

        ISchemaTypeReference WithScope(string? scope = null);

        ISchemaTypeReference WithNullable(bool[]? nullable = null);

        ISchemaTypeReference With(
            Optional<ITypeSystemMember> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default);
    }
}
