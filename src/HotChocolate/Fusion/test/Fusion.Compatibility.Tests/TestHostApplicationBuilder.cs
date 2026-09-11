using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// A minimal <see cref="IHostApplicationBuilder"/> test double. The repository does not
/// otherwise depend on the Microsoft.Extensions.Hosting package (only its Abstractions), so
/// this stands in for <c>Host.CreateApplicationBuilder()</c> without adding that dependency.
/// Only <see cref="Services"/> is functional; FusionServerAspNetCoreHostingBuilderExtensions
/// reads nothing else from the builder.
/// </summary>
public sealed class TestHostApplicationBuilder : IHostApplicationBuilder
{
    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public IConfigurationManager Configuration => throw new NotSupportedException();

    public IHostEnvironment Environment => throw new NotSupportedException();

    public ILoggingBuilder Logging => throw new NotSupportedException();

    public IMetricsBuilder Metrics => throw new NotSupportedException();

    public IServiceCollection Services { get; } = new ServiceCollection();

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
        => throw new NotSupportedException();
}
