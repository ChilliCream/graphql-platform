#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides empty collections for the types.
/// </summary>
public static class EmptyCollections
{
    /// <summary>
    /// Gets an empty read-only directive collection.
    /// </summary>
    public static IReadOnlyDirectiveCollection Directives
        => EmptyDirectiveCollection.Instance;

    /// <summary>
    /// Gets an empty read-only interface type definition collection.
    /// </summary>
    public static IReadOnlyInterfaceTypeDefinitionCollection InterfaceTypeDefinitions
        => EmptyInterfaceTypeDefinitionCollection.Instance;

    /// <summary>
    /// Gets an empty read-only input field definition collection.
    /// </summary>
    public static IReadOnlyFieldDefinitionCollection<IInputValueDefinition> InputFieldDefinitions
        => EmptyInputFieldDefinitionCollection.Instance;

    /// <summary>
    /// Gets an empty read-only output field definition collection.
    /// </summary>
    public static IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> OutputFieldDefinitions
        => EmptyOutputFieldDefinitionCollection.Instance;
}
