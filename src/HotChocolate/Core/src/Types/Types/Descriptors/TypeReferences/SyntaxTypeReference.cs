using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public sealed class SyntaxTypeReference
    : TypeReference
    , IEquatable<SyntaxTypeReference>
{
    public SyntaxTypeReference(
        ITypeNode type,
        TypeContext context,
        string? scope = null,
        Func<IDescriptorContext, TypeSystemObjectBase>? factory = null)
        : base(
            factory is null ? TypeReferenceKind.Syntax : TypeReferenceKind.Factory,
            context,
            scope)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = type.NamedType().Name.Value;
        Factory = factory;
    }

    /// <summary>
    /// Gets the name of the named type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the internal syntax type reference.
    /// </summary>
    public ITypeNode Type { get; }

    /// <summary>
    /// Gets a factory to create this type. Note, a factory is optional.
    /// </summary>
    public Func<IDescriptorContext, TypeSystemObjectBase>? Factory { get; }

    /// <inheritdoc />
    public bool Equals(SyntaxTypeReference? other)
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

        return Type.IsEqualTo(other.Type);
    }

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

        if (other is SyntaxTypeReference c)
        {
            return Equals(c);
        }

        return false;
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

        if (obj is SyntaxTypeReference c)
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
            return base.GetHashCode() ^ Type.GetHashCode() * 397;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Context}: {Type}";
    }

    public SyntaxTypeReference WithType(ITypeNode type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new SyntaxTypeReference(type, Context, Scope);
    }

    public SyntaxTypeReference WithContext(TypeContext context = TypeContext.None)
        => new(Type, context, Scope);

    public SyntaxTypeReference WithScope(string? scope = null)
        => new(Type, Context, scope);

    public SyntaxTypeReference WithFactory(
        Func<IDescriptorContext, TypeSystemObjectBase>? factory = null)
        => new(Type, Context, Scope, Factory);

    public SyntaxTypeReference With(
        Optional<ITypeNode> type = default,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default,
        Optional<Func<IDescriptorContext, TypeSystemObjectBase>?> factory = default)
    {
        if (type.HasValue && type.Value is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new SyntaxTypeReference(
            type.HasValue ? type.Value! : Type,
            context.HasValue ? context.Value : Context,
            scope.HasValue ? scope.Value : Scope,
            factory.HasValue ? factory.Value : Factory);
    }
}
