using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class TypeInfo
    : ITypeInfo
    , ITypeFactory
{
    private readonly IExtendedType _extendedType;

    private TypeInfo(
        Type namedType,
        Type originalType,
        IReadOnlyList<TypeComponent> components,
        bool isSchemaType,
        IExtendedType extendedType,
        bool isStructureValid)
    {
        NamedType = namedType;
        OriginalType = originalType;
        Components = components;
        IsSchemaType = isSchemaType;
        IsRuntimeType = !isSchemaType;
        _extendedType = extendedType;
        IsValid = isStructureValid;
    }

    /// <summary>
    /// Gets the type component that represents the named type.
    /// </summary>
    public Type NamedType { get; }

    /// <summary>
    /// Gets the original type from which this type info was inferred.
    /// </summary>
    public Type OriginalType { get; }

    /// <summary>
    /// The components represent the GraphQL type structure.
    /// </summary>
    public IReadOnlyList<TypeComponent> Components { get; }

    /// <summary>
    /// Defines if the <see cref="NamedType"/> is a GraphQL schema type.
    /// </summary>
    public bool IsSchemaType { get; }

    /// <summary>
    /// Defines if the <see cref="NamedType"/> is a runtime type.
    /// </summary>
    public bool IsRuntimeType { get; }

    /// <summary>
    /// Gets the extended type that contains information
    /// about type arguments and nullability.
    /// </summary>
    public IExtendedType GetExtendedType() => _extendedType;

    /// <summary>
    /// Defines if the component structure is valid in the GraphQL context.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// If this type is a schema type then this method defines if it is an input type.
    /// </summary>
    public bool IsInputType() =>
        IsSchemaType &&
        typeof(INamedInputType).IsAssignableFrom(NamedType);

    /// <summary>
    /// If this type is a schema type then this method defines if it is an output type.
    /// </summary>
    public bool IsOutputType() =>
        IsSchemaType &&
        typeof(INamedOutputType).IsAssignableFrom(NamedType);

    /// <summary>
    /// Creates a type structure with the <paramref name="namedType"/>.
    /// </summary>
    /// <param name="namedType">The named type component.</param>
    /// <returns>
    /// Returns a GraphQL type structure.
    /// </returns>
    public IType CreateType(INamedType namedType)
    {
        if (Components.Count == 1)
        {
            return namedType;
        }

        IType current = namedType;

        for (var i = Components.Count - 2; i >= 0; i--)
        {
            switch (Components[i].Kind)
            {
                case TypeComponentKind.Named:
                    throw new InvalidOperationException();

                case TypeComponentKind.NonNull:
                    current = new NonNullType(current);
                    break;

                case TypeComponentKind.List:
                    current = new ListType(current);
                    break;
            }
        }

        return current;
    }

    public static TypeInfo Create(IExtendedType type, TypeCache cache)
    {
        if (TryCreate(type, cache, out var typeInfo))
        {
            return typeInfo;
        }

        throw new NotSupportedException(
            "The provided type structure is not supported.");
    }

    public static bool TryCreate(
        IExtendedType type,
        TypeCache cache,
        [NotNullWhen(true)] out TypeInfo? typeInfo)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        typeInfo = cache.GetOrCreateTypeInfo(
            type,
            () => CreateInternal(type, type.Source, cache));

        typeInfo = typeInfo.IsValid ? typeInfo : null;
        return typeInfo is not null;
    }

    private static TypeInfo CreateInternal(
        IExtendedType type,
        Type originalType,
        TypeCache cache)
    {
        if (SchemaType.TryCreateTypeInfo(
            type,
            originalType,
            out var typeInfo))
        {
            return typeInfo;
        }

        if (RuntimeType.TryCreateTypeInfo(
            type,
            originalType,
            cache,
            out typeInfo))
        {
            return typeInfo;
        }

        throw new InvalidOperationException("Unable to create type info.");
    }

    private static bool IsStructureValid(IReadOnlyList<TypeComponent> components)
    {
        var nonNull = false;
        var named = false;
        var lists = 0;

        for (var i = 0; i < components.Count; i++)
        {
            if (named)
            {
                return false;
            }

            switch (components[i].Kind)
            {
                case TypeComponentKind.List:
                    nonNull = false;
                    lists++;

                    if (lists > 2)
                    {
                        return false;
                    }
                    break;

                case TypeComponentKind.NonNull when nonNull:
                    return false;

                case TypeComponentKind.NonNull:
                    nonNull = true;
                    break;

                case TypeComponentKind.Named:
                    nonNull = false;
                    named = true;
                    break;

                default:
                    throw new NotSupportedException(
                        "The type component kind is not supported.");
            }
        }

        return named;
    }
}
