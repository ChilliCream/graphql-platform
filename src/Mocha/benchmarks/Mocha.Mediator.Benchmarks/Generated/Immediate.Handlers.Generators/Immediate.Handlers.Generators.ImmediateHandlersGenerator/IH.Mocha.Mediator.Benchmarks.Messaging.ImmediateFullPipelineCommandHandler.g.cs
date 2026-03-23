using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1591

namespace Mocha.Mediator.Benchmarks.Messaging;

partial class ImmediateFullPipelineCommandHandler
{
	public sealed partial class Handler : global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.HandleBehavior _handleBehavior;
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePostBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> _immediateFullPipelinePostBehavior;
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineMainBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> _immediateFullPipelineMainBehavior;
		private readonly global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePreBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> _immediateFullPipelinePreBehavior;

		public Handler(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.HandleBehavior handleBehavior,
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePostBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> immediateFullPipelinePostBehavior,
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineMainBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> immediateFullPipelineMainBehavior,
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelinePreBehavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> immediateFullPipelinePreBehavior
		)
		{
			var handlerType = typeof(ImmediateFullPipelineCommandHandler);

			_handleBehavior = handleBehavior;

			_immediateFullPipelinePreBehavior = immediateFullPipelinePreBehavior;
			_immediateFullPipelinePreBehavior.HandlerType = handlerType;

			_immediateFullPipelineMainBehavior = immediateFullPipelineMainBehavior;
			_immediateFullPipelineMainBehavior.HandlerType = handlerType;

			_immediateFullPipelinePostBehavior = immediateFullPipelinePostBehavior;
			_immediateFullPipelinePostBehavior.HandlerType = handlerType;

			_immediateFullPipelinePostBehavior.SetInnerHandler(_handleBehavior);
			_immediateFullPipelineMainBehavior.SetInnerHandler(_immediateFullPipelinePostBehavior);
			_immediateFullPipelinePreBehavior.SetInnerHandler(_immediateFullPipelineMainBehavior);
		}

		public async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken = default
		)
		{
			return await _immediateFullPipelinePreBehavior
				.HandleAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public sealed class HandleBehavior : global::Immediate.Handlers.Shared.Behavior<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>
	{

		public HandleBehavior(
		)
		{
		}

		public override async global::System.Threading.Tasks.ValueTask<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse> HandleAsync(
			global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command request,
			global::System.Threading.CancellationToken cancellationToken
		)
		{
			return await global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler
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
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Handler), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Immediate.Handlers.Shared.IHandler<global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Command, global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.Handler), lifetime));
		services.Add(new(typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.HandleBehavior), typeof(global::Mocha.Mediator.Benchmarks.Messaging.ImmediateFullPipelineCommandHandler.HandleBehavior), lifetime));
		return services;
	}
}
