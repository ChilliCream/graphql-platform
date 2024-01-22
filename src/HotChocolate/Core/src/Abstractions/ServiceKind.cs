using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate;

/// <summary>
/// Represents the way a service is injected and handled by the execution engine.
/// </summary>
public enum ServiceKind
{
    /// <summary>
    /// The execution engine will retrieve the service from a
    /// <see cref=" IServiceProvider" /> and inject it into the
    /// annotated parameter.
    /// </summary>
    Default,

    /// <summary>
    /// The service will be retrieved from the <see cref="IServiceProvider" />
    /// but can only accessed by a single resolver at a time.
    /// Example for such service is for instance the Entity
    /// Framework DbContext when scoped on the request.
    /// </summary>
    Synchronized,

    /// <summary>
    /// A service is rented for each resolver execution.
    /// Pooled services need to be registered as <see cref="ObjectPool{T}"/>
    /// on the resolver's <see cref=" IServiceProvider" />.
    /// </summary>
    Pooled,

    /// <summary>
    /// A service that is retrieved from a IServiceScope that is bound to the resolver
    /// execution.
    /// </summary>
    Resolver,
}
