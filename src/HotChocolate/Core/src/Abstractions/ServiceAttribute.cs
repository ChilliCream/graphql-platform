namespace HotChocolate;

/// <summary>
/// Marks a resolver parameter as a service that shall be injected by the execution engine.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class ServiceAttribute : Attribute
{
    /// <summary>
    /// Marks a resolver parameter as a service that shall be injected by the execution engine.
    /// </summary>
    public ServiceAttribute()
    {
    }

    /// <summary>
    /// Marks a resolver parameter as a service that shall be injected by the execution engine.
    /// </summary>
    /// <param name="key">
    /// A key that shall be used to resolve the service.
    /// </param>
    public ServiceAttribute(string key)
    {
        ServiceKey = key;
    }

    /// <summary>
    /// Marks a resolver parameter as a service that shall be injected by the execution engine.
    /// </summary>
    /// <param name="key">
    /// A key that shall be used to resolve the service.
    /// </param>
    public ServiceAttribute(object key)
    {
        ServiceKey = key;
    }

    /// <summary>
    /// Gets the string service key, or <see langword="null"/> when no key is specified or the service key is not a string.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ServiceKey"/> to access service keys of all types.
    /// </remarks>
    [Obsolete("Use ServiceKey instead. Key returns null for non-string keys.")]
    public string? Key => ServiceKey as string;

    /// <summary>
    /// Gets the service key, or <see langword="null"/> when no key is specified.
    /// </summary>
    public object? ServiceKey { get; }
}
