using System;
using System.Net.Http;
using StrawberryShake.Configuration;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorConfiguration
        : IOperationExecutionConfiguration
    {
        private Func<IServiceProvider, HttpClient>? _clientFactory;
        private Func<IServiceProvider, OperationDelegate>? _pipelineFactory;

        public HttpOperationExecutorConfiguration WithHttpClient(
            Func<IServiceProvider, HttpClient> clientFactory)
        {
            if (clientFactory is null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            _clientFactory = clientFactory;
            return this;
        }

        public HttpOperationExecutorConfiguration UsePipeline(
            Func<IServiceProvider, OperationDelegate> pipelineFactory)
        {
            if (pipelineFactory is null)
            {
                throw new ArgumentNullException(nameof(pipelineFactory));
            }

            _pipelineFactory = pipelineFactory;
            return this;
        }

        ExecutorKind IOperationExecutionConfiguration.Kind => ExecutorKind.Default;

        void IOperationExecutionConfiguration.Apply(
            IServiceConfiguration services,
            string schemaName)
        {

        }
    }



    /*

        services.AddOperationExecutor("StarWars")
            .Http(c => c.NamedClient("Foo"))

        services.AddOperationExecutor("StarWars")
            .Http(c => c.HttpClient(s => s.Get...))

        services.AddOperationExecutor("StarWars")
            .Http(c => c.HttpClient(new HttpClient))

        services.AddOperationExecutor("StarWars")
            .Http(c => c.UseNamedClient("Foo").DefinePipeline(p => p.FooBar()))

        services.AddOperationExecutor("StarWars")
            .Http(c => c.UseNamedClient("Foo").UseDefaultPipeline())

    */

    /*
        services.AddOperationExecutor(
            "StarWars",
            b =>
            {
                b.UseHttp() => IHttpOperationExecutionBuilder
                    .WithNamedHttpClient("Foo")
                    .WithHttpClient()
                    .WithHttpClient(new HttpClient())
                    .WithHttpClient(s => ....)
                    .UseDefaultPipeline()
                    .And() => IOperationExecutionConfiguration
                    .UseWebSockets("ws://blub") => IWebSocketOperationStreamExecutionBuilder
                    .And()
                    .UseHttpBatching() => IHttpOperationBatchExecutionBuilder
                    .WithNamedClient()
                    .UseGrpc(); => IGrpcOperationExecutionBuilder
            });
    */
}
