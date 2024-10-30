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
        Key = key;
    }

    /// <summary>
    /// Gets the key that shall be used to resolve the service.
    /// </summary>
    public string? Key { get; }
}
