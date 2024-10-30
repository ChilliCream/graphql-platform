using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A reference to a type that has not yet been create by name.
/// This reference contains the type name plus a factory to create it.
/// </summary>
public sealed class DependantFactoryTypeReference
    : TypeReference
    , IEquatable<DependantFactoryTypeReference>
{
    internal DependantFactoryTypeReference(
        string name,
        TypeReference dependency,
        Func<IDescriptorContext, TypeSystemObjectBase> factory,
        TypeContext context,
        string? scope = null)
        : base(
            TypeReferenceKind.DependantFactory,
            context,
            scope)
    {
        Name = name.EnsureGraphQLName();
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Gets the name of this reference.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the reference to the type this type is dependant on.
    /// </summary>
    public TypeReference Dependency { get; }

    /// <summary>
    /// Gets a factory to create this type.
    /// </summary>
    public Func<IDescriptorContext, TypeSystemObjectBase> Factory { get; }

    /// <inheritdoc />
    public override bool Equals(TypeReference? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is DependantFactoryTypeReference c)
        {
            return Equals(c);
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(DependantFactoryTypeReference? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!IsEqual(other))
        {
            return false;
        }

        return Name.EqualsOrdinal(other.Name) && Dependency.Equals(other.Dependency);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is DependantFactoryTypeReference c)
        {
            return Equals(c);
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return base.GetHashCode() ^
                Name.GetHashCode() * 397 ^
                Dependency.GetHashCode() * 397;
        }
    }

    /// <inheritdoc />
    public override string ToString()
        => ToString($"{Name}->{Dependency}");

    public DependantFactoryTypeReference With(
        Optional<string> name = default,
        Optional<TypeReference> dependency = default,
        Optional<Func<IDescriptorContext, TypeSystemObjectBase>> factory = default,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default)
    {
        return new DependantFactoryTypeReference(
            name.HasValue ? name.Value! : Name,
            dependency.HasValue ? dependency.Value! : Dependency,
            factory.HasValue ? factory.Value! : Factory,
            context.HasValue ? context.Value : Context,
            scope.HasValue ? scope.Value! : Scope);
    }
}
