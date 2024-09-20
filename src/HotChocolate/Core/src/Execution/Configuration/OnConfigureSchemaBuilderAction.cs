namespace HotChocolate.Execution.Configuration;

/// <summary>
/// This struct is used as a oneOf to allow for both a sync and an async hook
/// into the schema builder configuration.
/// </summary>
public readonly struct OnConfigureSchemaBuilderAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="OnConfigureSchemaBuilderAction"/>.
    /// </summary>
    /// <param name="action">
    /// The synchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="action"/> is <c>null</c>.
    /// </exception>
    public OnConfigureSchemaBuilderAction(OnConfigureSchemaBuilder action)
    {
        Configure = action ?? throw new ArgumentNullException(nameof(action));
        ConfigureAsync = default;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OnConfigureSchemaBuilderAction"/>.
    /// </summary>
    /// <param name="asyncAction">
    /// The asynchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="asyncAction"/> is <c>null</c>.
    /// </exception>
    public OnConfigureSchemaBuilderAction(OnConfigureSchemaBuilderAsync asyncAction)
    {
        Configure = default;
        ConfigureAsync = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
    }

    /// <summary>
    /// Gets the synchronous action.
    /// </summary>
    public OnConfigureSchemaBuilder? Configure { get; }

    /// <summary>
    /// Gets the asynchronous action.
    /// </summary>
    public OnConfigureSchemaBuilderAsync? ConfigureAsync { get; }
}

/// <summary>
/// This delegate is used to configure the schema builder.
/// </summary>
public delegate void OnConfigureSchemaBuilder(
    ConfigurationContext context,
    IServiceProvider schemaServices);

/// <summary>
/// This delegate is used to configure the schema builder.
/// </summary>
public delegate ValueTask OnConfigureSchemaBuilderAsync(
    ConfigurationContext context,
    IServiceProvider schemaServices,
    CancellationToken cancellationToken);
