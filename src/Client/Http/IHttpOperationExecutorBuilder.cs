using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;
using System.Collections.Generic;

namespace StrawberryShake.Http
{
    public interface IHttpOperationExecutorBuilder
    {
        IHttpOperationExecutorBuilder SetClient(Func<IServiceProvider, Func<HttpClient>> clientFactory);

        IHttpOperationExecutorBuilder SetPipeline(Func<IServiceProvider, OperationDelegate> pipelineFactory);

        IHttpOperationExecutorBuilder AddServices(IServiceProvider services);

        IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance);

        IOperationExecutor Build();
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
