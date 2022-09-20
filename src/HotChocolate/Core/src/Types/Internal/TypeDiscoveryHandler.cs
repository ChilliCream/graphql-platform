#nullable enable
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// A type discover handler allows to specify how types are inferred
/// from <see cref="ExtendedTypeReference"/>s.
/// </summary>
public abstract class TypeDiscoveryHandler
{
    /// <summary>
    /// Tries to infer a type from the <paramref name="typeReference"/>.
    /// </summary>
    /// <param name="typeReference">
    /// The runtime type reference.
    /// </param>
    /// <param name="typeReferenceInfo">
    /// The runtime type reference info provides addition metadata
    /// around the <paramref name="typeReference"/>.
    /// </param>
    /// <param name="schemaTypeRefs">
    /// The schema types that were inferred from the type reference.
    /// </param>
    /// <returns>
    /// <c>true</c> if the handler was able to infer at least one schema type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public abstract bool TryInferType(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        [NotNullWhen(true)] out ITypeReference[]? schemaTypeRefs);

    /// <summary>
    /// Tries to infer the <see cref="TypeKind"/> from a runtime reference.
    /// </summary>
    /// <param name="typeReference">
    /// The runtime type reference.
    /// </param>
    /// <param name="typeReferenceInfo">
    /// The runtime type reference info provides addition metadata
    /// around the <paramref name="typeReference"/>.
    /// </param>
    /// <param name="typeKind">
    /// The predicted type kind.
    /// </param>
    /// <returns>
    /// <c>true</c> if the handler was able to infer the <paramref name="typeKind"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public virtual bool TryInferKind(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        out TypeKind typeKind)
    {
        typeKind = default;
        return false;
    }
}
