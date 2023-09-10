using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

/// <summary>
/// Represents the way a <see cref="DbContext"/> is injected and handled by the execution engine.
/// </summary>
public enum DbContextKind
{
    /// <summary>
    /// The <see cref="DbContext"/> is configured as a scoped service per request and
    /// will be retrieved from the <see cref="IServiceProvider" />.
    /// The execution engine will ensure that this request instance of the <see cref="DbContext"/>
    /// can only accessed by a single resolver at a time.
    /// This is the default behavior when working with entity framework.
    /// </summary>
    Synchronized,

    /// <summary>
    /// The <see cref="DbContext"/> is rented through the <see cref="IDbContextFactory{TContext}"/>
    /// and returned by disposing it at the end of the resolver execution.
    /// This <see cref="DbContext"/>  behavior should be used for better throughput.
    /// </summary>
    Pooled,

    /// <summary>
    /// Resolvers that use a <see cref="DbContext"/> will create their own
    /// <see cref="IServiceScope"/> when executing. The <see cref="DbContext"/> is retrieved from
    /// the service scope and the scope is disposed at the end of the resolver execution.
    /// </summary>
    Resolver
}
