namespace HotChocolate.Types;

/// <summary>
/// A complex output type can be an <see cref="IObjectTypeDefinition" />
/// or an <see cref="IInterfaceTypeDefinition" />.
/// </summary>
public interface IComplexTypeDefinition : IOutputTypeDefinition
{
    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    IReadOnlyInterfaceTypeDefinitionCollection Implements { get; }

    /// <summary>
    /// Gets the field definitions of this type.
    /// </summary>
    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> Fields { get; }

    /// <summary>
    /// Defines if this type is implementing an interface
    /// with the given <paramref name="typeName" />.
    /// </summary>
    /// <param name="typeName">
    /// The interface type name.
    /// </param>
    bool IsImplementing(string typeName);

    /// <summary>
    /// Defines if this type is implementing
    /// the given <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="interfaceType">
    /// The interface type.
    /// </param>
    bool IsImplementing(IInterfaceTypeDefinition interfaceType);
}
