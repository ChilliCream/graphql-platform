using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1591

namespace Mocha.Mediator.Benchmarks.Messaging;

partial class ImmediateCommandHandler
{
	public sealed partial class Handler : global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.HandleBehavior _handleBehavior;

		public Handler(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.HandleBehavior handleBehavior
		)
		{
			var handlerType = typeof(ImmediateCommandHandler);

			_handleBehavior = handleBehavior;

		}

		public async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken = default
		)
		{
			return await _handleBehavior
				.HandleAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public sealed class HandleBehavior : global::Immediate.Handlers.Shared.Behavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{

		public HandleBehavior(
		)
		{
		}

		public override async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken
		)
		{
			return await global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler
				.HandleAsync(
					request
					, cancellationToken
				)
				.ConfigureAwait(false);
		}
	}

	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public static IServiceCollection AddHandlers(
		IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped
	)
	{
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Handler), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.HandleBehavior), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateCommandHandler.HandleBehavior), lifetime));
		return services;
	}
}
