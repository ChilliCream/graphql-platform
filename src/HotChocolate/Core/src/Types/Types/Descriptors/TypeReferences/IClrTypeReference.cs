using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
        , IEquatable<IClrTypeReference>
    {
        Type Type { get; }

        IClrTypeReference Rewrite();

        IClrTypeReference WithType(Type type);

        IClrTypeReference WithContext(TypeContext context = TypeContext.None);

        IClrTypeReference WithScope(string? scope = null);

        IClrTypeReference WithNullable(bool[]? nullable = null);

        IClrTypeReference With(
            Optional<Type> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default);
    }
}
