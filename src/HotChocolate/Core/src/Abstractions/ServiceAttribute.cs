using System;

namespace HotChocolate;

/// <summary>
/// Marks a resolver parameter as a service that shall be injected by the execution engine.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ServiceAttribute : Attribute
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

    public ServiceKind Kind { get; }
}
