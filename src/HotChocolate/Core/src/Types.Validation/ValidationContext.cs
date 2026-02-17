using HotChocolate.Logging.Contracts;

namespace HotChocolate;

/// <summary>
/// Represents the context for schema validation.
/// </summary>
/// <param name="schema">The schema to be validated.</param>
/// <param name="validationLog">The log for recording validation issues.</param>
public sealed class ValidationContext(
    ISchemaDefinition schema,
    IValidationLog validationLog)
{
    /// <summary>
    /// Gets the schema to be validated.
    /// </summary>
    public ISchemaDefinition Schema { get; } = schema;

    /// <summary>
    /// Gets the validation log.
    /// </summary>
    public IValidationLog Log { get; } = validationLog;
}
