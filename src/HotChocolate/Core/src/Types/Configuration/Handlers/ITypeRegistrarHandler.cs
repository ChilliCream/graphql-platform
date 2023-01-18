using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// The type registrar handler will process a type reference.
/// The handler may or may not create a type instance from the provided type reference
/// and register it with the <see cref="ITypeRegistrar"/>.
/// </summary>
internal interface ITypeRegistrarHandler
{
    /// <summary>
    /// The type reference kind that can be handled.
    /// </summary>
    TypeReferenceKind Kind { get; }

    /// <summary>
    /// Handles the type reference.
    /// </summary>
    /// <param name="typeRegistrar">
    /// The type registrar that can be used to register types.
    /// </param>
    /// <param name="typeReference">
    /// The type reference.
    /// </param>
    void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference);
}
