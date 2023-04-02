using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// The configuration context is used during the setup of the schema and request executor.
/// </summary>
public sealed class ConfigurationContext : IHasContextData
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationContext"/>.
    /// </summary>
    /// <param name="schemaName">
    /// The schema name.
    /// </param>
    /// <param name="schemaBuilder">
    /// The schema builder that is used to create the schema.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="schemaBuilder"/> is <c>null</c>.
    /// </exception>
    public ConfigurationContext(string schemaName, ISchemaBuilder schemaBuilder)
    {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        SchemaBuilder = schemaBuilder ?? throw new ArgumentNullException(nameof(schemaBuilder));
    }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the schema builder that is used to create the schema.
    /// </summary>
    public ISchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Gets the configuration context data which can be used by hooks to store arbitrary state.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
