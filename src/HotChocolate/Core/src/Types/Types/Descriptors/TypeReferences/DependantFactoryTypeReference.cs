using System;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public sealed class DependantFactoryTypeReference
    : TypeReference
    , IEquatable<DependantFactoryTypeReference>
{
    public DependantFactoryTypeReference(
        NameString name,
        ITypeReference dependency,
        Func<IDescriptorContext, TypeSystemObjectBase> factory,
        TypeContext context,
        string? scope = null)
        : base(
            TypeReferenceKind.DependantFactory,
            context,
            scope)
    {
        Name = name.EnsureNotEmpty(nameof(name));
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Gets the name of this reference.
    /// </summary>
    public NameString Name { get; }

    /// <summary>
    /// Gets the reference to the type this type is dependant on.
    /// </summary>
    public ITypeReference Dependency { get; }

    /// <summary>
    /// Gets a factory to create this type.
    /// </summary>
    public Func<IDescriptorContext, TypeSystemObjectBase> Factory { get; }

    /// <inheritdoc />
    public override bool Equals(ITypeReference? other)
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

        return Name.Equals(other.Name) && Dependency.Equals(other.Dependency);
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
        => $"{Context}: {Name}->{Dependency}";
}
