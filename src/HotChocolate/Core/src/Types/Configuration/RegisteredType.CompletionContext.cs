using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class RegisteredType : ITypeCompletionContext
{
    private TypeReferenceResolver? _typeReferenceResolver;

    public TypeStatus Status { get; set; } = TypeStatus.Initialized;

    /// <inheritdoc />
    public bool? IsQueryType { get; set; }

    /// <inheritdoc />
    public bool? IsMutationType { get; set; }

    /// <inheritdoc />
    public bool? IsSubscriptionType { get; set; }

    /// <summary>
    /// Global middleware components.
    /// </summary>
    public List<FieldMiddleware>? GlobalComponents { get; private set; }

    /// <inheritdoc />
    IReadOnlyList<FieldMiddleware> ITypeCompletionContext.GlobalComponents
        => GlobalComponents ?? (IReadOnlyList<FieldMiddleware>)Array.Empty<FieldMiddleware>();

    /// <inheritdoc />
    public IsOfTypeFallback? IsOfType { get; private set; }

    public ITypeReference TypeReference => References[0];

    public void PrepareForCompletion(
        TypeReferenceResolver typeReferenceResolver,
        List<FieldMiddleware> globalComponents,
        IsOfTypeFallback? isOfType)
    {
        _typeReferenceResolver = typeReferenceResolver;
        GlobalComponents = globalComponents;
        IsOfType = isOfType;
    }

    /// <inheritdoc />
    public bool TryGetType<T>(
        ITypeReference typeRef,
        [NotNullWhen(true)] out T? type)
        where T : IType
    {
        if (_typeReferenceResolver is null)
        {
            throw new InvalidOperationException(RegisteredType_Completion_NotYetReady);
        }

        if (_typeReferenceResolver.TryGetType(typeRef, out IType? t) &&
            t is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    /// <inheritdoc />
    public T GetType<T>(ITypeReference typeRef) where T : IType
    {
        if (typeRef is null)
        {
            throw new ArgumentNullException(nameof(typeRef));
        }

        if (!TryGetType(typeRef, out T? type))
        {
            throw TypeCompletionContext_UnableToResolveType(Type, typeRef);
        }

        return type;
    }

    /// <inheritdoc />
    public ITypeReference GetNamedTypeReference(ITypeReference typeRef)
    {
        if (_typeReferenceResolver is null)
        {
            throw new InvalidOperationException(RegisteredType_Completion_NotYetReady);
        }

        return _typeReferenceResolver.GetNamedTypeReference(typeRef);
    }

    /// <inheritdoc />
    public IEnumerable<T> GetTypes<T>() where T : IType
    {
        if (_typeReferenceResolver is null)
        {
            throw new InvalidOperationException(RegisteredType_Completion_NotYetReady);
        }

        if (Status == TypeStatus.Initialized)
        {
            throw new NotSupportedException();
        }

        return _typeReferenceResolver.GetTypes<T>();
    }

    /// <inheritdoc />
    public bool TryGetDirectiveType(IDirectiveReference directiveRef,
        [NotNullWhen(true)] out DirectiveType? directiveType)
    {
        if (_typeReferenceResolver is null)
        {
            throw new InvalidOperationException(RegisteredType_Completion_NotYetReady);
        }

        return _typeReferenceResolver.TryGetDirectiveType(directiveRef, out directiveType);
    }
}
