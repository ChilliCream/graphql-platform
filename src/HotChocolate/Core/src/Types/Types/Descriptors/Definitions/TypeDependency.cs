using System;
using HotChocolate.Internal;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public sealed class TypeDependency
{
    public TypeDependency(
        ITypeReference type,
        TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Fulfilled = fulfilled;
    }

    public TypeDependencyFulfilled Fulfilled { get; }

    public ITypeReference Type { get; }

    public TypeDependency With(
        ITypeReference? typeReference = null,
        TypeDependencyFulfilled? kind = null)
        => new(typeReference ?? Type, kind ?? Fulfilled);

    public static TypeDependency FromSchemaType(
        IExtendedType type,
        TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (!type.IsSchemaType)
        {
            throw new ArgumentException(
                TypeResources.TypeDependency_MustBeSchemaType,
                nameof(type));
        }

        return new TypeDependency(TypeReference.Create(type), fulfilled);
    }
}
