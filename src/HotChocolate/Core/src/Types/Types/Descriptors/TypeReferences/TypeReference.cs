using System;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Types.Descriptors.SchemaTypeReference;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A type reference is used to refer to a type in the type system.
/// This allows us to loosely couple types during schema creation.
/// </summary>
public abstract class TypeReference : ITypeReference
{
    protected TypeReference(
        TypeReferenceKind kind,
        TypeContext context,
        string? scope)
    {
        Kind = kind;
        Context = context;
        Scope = scope;
    }

    /// <inheritdoc />
    public TypeReferenceKind Kind { get; }

    /// <inheritdoc />
    public TypeContext Context { get; }

    /// <inheritdoc />
    public string? Scope { get; }

    protected bool IsEqual(ITypeReference other)
    {
        if (Context != other.Context
            && Context != TypeContext.None
            && other.Context != TypeContext.None)
        {
            return false;
        }

        if (!Scope.EqualsOrdinal(other.Scope))
        {
            return false;
        }

        return true;
    }

    public abstract bool Equals(ITypeReference? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, null))
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        return Equals(obj as ITypeReference);
    }

    public override int GetHashCode()
        => HashCode.Combine(Scope);

    public static DependantFactoryTypeReference Create(
        string name,
        ITypeReference dependency,
        Func<IDescriptorContext, TypeSystemObjectBase> factory,
        TypeContext context = TypeContext.None,
        string? scope = null)
        => new(name, dependency, factory, context, scope);

    public static SchemaTypeReference Create(
        ITypeSystemMember type,
        string? scope = null)
    {
        if (scope is null && type is IHasScope { Scope: not null } withScope)
        {
            scope = withScope.Scope;
        }
        return new SchemaTypeReference(type, scope: scope);
    }

    public static SyntaxTypeReference Create(
        ITypeNode type,
        TypeContext context = TypeContext.None,
        string? scope = null,
        Func<IDescriptorContext, TypeSystemObjectBase>? factory = null) =>
        new(type, context, scope, factory);

    public static SyntaxTypeReference Create(
        string typeName,
        TypeContext context = TypeContext.None,
        string? scope = null,
        Func<IDescriptorContext, TypeSystemObjectBase>? factory = null) =>
        new(new NamedTypeNode(typeName.EnsureGraphQLName()), context, scope, factory);

    public static SyntaxTypeReference Parse(
        string sourceText,
        TypeContext context = TypeContext.None,
        string? scope = null,
        Func<IDescriptorContext, TypeSystemObjectBase>? factory = null) =>
        new(Utf8GraphQLParser.Syntax.ParseTypeReference(sourceText), context, scope, factory);

    public static ExtendedTypeReference Create(
        IExtendedType type,
        TypeContext context = TypeContext.None,
        string? scope = null)
    {
        if (type.IsSchemaType)
        {
            return new ExtendedTypeReference(
                type,
                InferTypeContext(type),
                scope);
        }

        return new ExtendedTypeReference(type, context, scope);
    }
}
