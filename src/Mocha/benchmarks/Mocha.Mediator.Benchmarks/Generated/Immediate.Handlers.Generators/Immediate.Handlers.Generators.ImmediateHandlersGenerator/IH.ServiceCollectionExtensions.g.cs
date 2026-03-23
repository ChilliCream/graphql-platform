using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable CS1591

namespace Mocha.Mediator.Benchmarks;

public static class HandlerServiceCollectionExtensions
{
	public static IServiceCollection AddMochaMediatorBenchmarksBehaviors(
		this IServiceCollection services)
	{
		services.TryAddTransient(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePreBehavior<,>));
		services.TryAddTransient(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineMainBehavior<,>));
		services.TryAddTransient(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePostBehavior<,>));
		services.TryAddTransient(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateBenchmarkBehavior<,>));
		
		return services;
	}

	public static IServiceCollection AddMochaMediatorBenchmarksHandlers(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped
	)
	{
		global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.AddHandlers(services, lifetime);
		global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.AddHandlers(services, lifetime);
		global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.AddHandlers(services, lifetime);
		
		return services;
	}
}
