using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1591

namespace Mocha.Mediator.Benchmarks.Messaging;

partial class ImmediatePipelineCommandHandler
{
	public sealed partial class Handler : global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.HandleBehavior _handleBehavior;
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateBenchmarkBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> _immediateBenchmarkBehavior;

		public Handler(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.HandleBehavior handleBehavior,
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateBenchmarkBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> immediateBenchmarkBehavior
		)
		{
			var handlerType = typeof(ImmediatePipelineCommandHandler);

			_handleBehavior = handleBehavior;

			_immediateBenchmarkBehavior = immediateBenchmarkBehavior;
			_immediateBenchmarkBehavior.HandlerType = handlerType;

			_immediateBenchmarkBehavior.SetInnerHandler(_handleBehavior);
		}

		public async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken = default
		)
		{
			return await _immediateBenchmarkBehavior
				.HandleAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public sealed class HandleBehavior : global::Immediate.Handlers.Shared.Behavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{

		public HandleBehavior(
		)
		{
		}

		public override async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken
		)
		{
			return await global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler
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
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Handler), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.HandleBehavior), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediatePipelineCommandHandler.HandleBehavior), lifetime));
		return services;
	}
}
