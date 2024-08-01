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
    /// <param name="applicationServices">
    /// The application services.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="schemaBuilder"/> is <c>null</c>.
    /// </exception>
    public ConfigurationContext(
        string schemaName,
        ISchemaBuilder schemaBuilder,
        IServiceProvider applicationServices)
    {
        SchemaName = schemaName ??
            throw new ArgumentNullException(nameof(schemaName));
        SchemaBuilder = schemaBuilder ??
            throw new ArgumentNullException(nameof(schemaBuilder));
        ApplicationServices = applicationServices ??
            throw new ArgumentNullException(nameof(applicationServices));
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
    /// Gets the application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; }

    /// <summary>
    /// Gets the configuration context data which can be used by hooks to store arbitrary state.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
