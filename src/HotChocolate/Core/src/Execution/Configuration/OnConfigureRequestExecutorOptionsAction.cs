using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// This struct is used as a oneOf to allow for both a sync and ab async hook
/// into the request executor option finalization.
/// </summary>
public readonly struct OnConfigureRequestExecutorOptionsAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="OnConfigureRequestExecutorOptionsAction"/>.
    /// </summary>
    /// <param name="action">
    /// The synchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="action"/> is <c>null</c>.
    /// </exception>
    public OnConfigureRequestExecutorOptionsAction(OnConfigureRequestExecutorOptions action)
    {
        Configure = action ?? throw new ArgumentNullException(nameof(action));
        ConfigureAsync = default;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OnConfigureRequestExecutorOptionsAction"/>.
    /// </summary>
    /// <param name="async">
    /// The asynchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="async"/> is <c>null</c>.
    /// </exception>
    public OnConfigureRequestExecutorOptionsAction(OnConfigureRequestExecutorOptionsAsync async)
    {
        Configure = default;
        ConfigureAsync = async ?? throw new ArgumentNullException(nameof(async));
    }

    /// <summary>
    /// Gets the synchronous action.
    /// </summary>
    public OnConfigureRequestExecutorOptions? Configure { get; }

    /// <summary>
    /// Gets the asynchronous action.
    /// </summary>
    public OnConfigureRequestExecutorOptionsAsync? ConfigureAsync { get; }
}

/// <summary>
/// This delegate is used to configure the request executor options.
/// </summary>
public delegate void OnConfigureRequestExecutorOptions(
    ConfigurationContext context,
    RequestExecutorOptions options);

/// <summary>
/// This delegate is used to configure the request executor options.
/// </summary>
public delegate ValueTask OnConfigureRequestExecutorOptionsAsync(
    ConfigurationContext context,
    RequestExecutorOptions options,
    CancellationToken cancellationToken);
