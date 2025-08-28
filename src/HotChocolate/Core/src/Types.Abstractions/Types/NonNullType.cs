using HotChocolate.Language;
using static HotChocolate.Properties.TypesAbstractionResources;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

/// <summary>
/// Represents a non-null type in GraphQL.
/// https://spec.graphql.org/October2021/#sec-Non-Null
/// </summary>
public sealed class NonNullType : IWrapperType, ISyntaxNodeProvider<NonNullTypeNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonNullType"/> class.
    /// </summary>
    /// <param name="nullableType">
    /// The type that is wrapped by the non-null type.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The <paramref name="nullableType"/> is a non-null type.
    /// </exception>
    public NonNullType(IType nullableType)
    {
        ArgumentNullException.ThrowIfNull(nullableType);

        if (nullableType.Kind is TypeKind.NonNull)
        {
            throw new ArgumentException(
                NonNullType_InnerTypeCannotBeNonNull,
                nameof(nullableType));
        }

        NullableType = nullableType;
    }

    /// <summary>
    /// Gets the kind of the type.
    /// </summary>
    public TypeKind Kind => TypeKind.NonNull;

    /// <summary>
    /// Gets the type that is wrapped by the non-null type.
    /// </summary>
    public IType NullableType { get; }

    IType IWrapperType.InnerType => NullableType;

    public override string ToString()
        => FormatTypeRef(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="NonNullTypeNode"/> from the current <see cref="NonNullType"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="NonNullTypeNode"/>.
    /// </returns>
    public NonNullTypeNode ToSyntaxNode()
        => (NonNullTypeNode)FormatTypeRef(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => FormatTypeRef(this);

    /// <summary>
    /// Determines whether the current type is equal to another type.
    /// </summary>
    /// <param name="other">
    /// The type to compare with the current type.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current type is equal to the other type; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    /// <summary>
    /// Determines whether the current type is equal to another type.
    /// </summary>
    /// <param name="other">
    /// The type to compare with the current type.
    /// </param>
    /// <param name="comparison">
    /// The type comparison.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current type is equal to the other type; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is NonNullType otherNonNull
            && NullableType.Equals(otherNonNull.NullableType, comparison);
    }
}
