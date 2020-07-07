using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
    {
        Type Type { get; }

        IClrTypeReference Compile();

        new IClrTypeReference WithContext(TypeContext context = TypeContext.None);

        new IClrTypeReference WithScope(string? scope = null);

        IClrTypeReference WithType(Type type);

        IClrTypeReference With(
            Optional<Type> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default);
    }
}
