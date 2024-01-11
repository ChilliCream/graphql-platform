using System;

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
    /// <param name="kind">
    /// The scope of the service.
    /// </param>
    public ServiceAttribute(ServiceKind kind = ServiceKind.Default)
    {
        Kind = kind;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Marks a resolver parameter as a service that shall be injected by the execution engine.
    /// </summary>
    /// <param name="key">
    /// A key that shall be used to resolve the service.
    /// </param>
    /// <param name="kind">
    /// The scope of the service.
    /// </param>
    public ServiceAttribute(string key, ServiceKind kind = ServiceKind.Default)
    {
        Key = key;
        Kind = kind;
    }
    
    /// <summary>
    /// Gets the key that shall be used to resolve the service.
    /// </summary>
    public string? Key { get; }
#endif

    /// <summary>
    /// Gets the service kind which specifies the way the service
    /// shall be injected and handled by the execution engine.
    /// </summary>
    public ServiceKind Kind { get; }
}
