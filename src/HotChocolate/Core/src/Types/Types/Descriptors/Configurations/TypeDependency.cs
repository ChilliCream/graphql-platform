using HotChocolate.Internal;
using HotChocolate.Properties;

namespace HotChocolate.Types.Descriptors.Configurations;

public sealed class TypeDependency
{
    public TypeDependency(TypeReference type, TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Fulfilled = fulfilled;
    }

    public TypeDependencyFulfilled Fulfilled { get; }

    public TypeReference Type { get; }

    public TypeDependency With(
        TypeReference? typeReference = null,
        TypeDependencyFulfilled? kind = null)
        => new(typeReference ?? Type, kind ?? Fulfilled);

    public static TypeDependency FromSchemaType(
        IExtendedType type,
        TypeDependencyFulfilled fulfilled = TypeDependencyFulfilled.Default)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsSchemaType)
        {
            throw new ArgumentException(
                TypeResources.TypeDependency_MustBeSchemaType,
                nameof(type));
        }

        return new TypeDependency(TypeReference.Create(type), fulfilled);
    }
}
