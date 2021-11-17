#nullable enable

using System;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A type reference is used to refer to a type in the type system.
/// This allows us to loosely couple types during schema creation.
/// </summary>
public interface ITypeReference : IEquatable<ITypeReference>
{
    /// <summary>
    /// Gets the kind of type reference.
    /// </summary>
    TypeReferenceKind Kind { get; }

    /// <summary>
    /// Gets the context in which the type reference was created.
    /// </summary>
    TypeContext Context { get; }

    /// <summary>
    /// Gets the scope in which the type reference was created.
    /// </summary>
    string? Scope { get; }
}
