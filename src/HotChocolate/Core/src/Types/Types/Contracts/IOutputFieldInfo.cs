namespace HotChocolate.Types;

/// <summary>
/// This interface aggregates the most important attributes of an output-field definition.
/// </summary>
public interface IOutputFieldInfo : INameProvider, ISchemaCoordinateProvider, IRuntimeTypeProvider
{
    /// <summary>
    /// Gets the return type of this field.
    /// </summary>
    IOutputType Type { get; }

    /// <summary>
    /// Gets the field arguments.
    /// </summary>
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }
}
