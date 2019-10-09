using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using HotChocolate.AspNetCore.Grpc;

namespace StarWars
{
    /// <summary>
    /// Test the gRPC client with the gRPC GraphQL service
    /// </summary>
    public class TestGrpcToGraphqlHostedService
        : IHostedService, IDisposable
    {
        private readonly ILogger<TestGrpcToGraphqlHostedService> logger;
        private readonly IHostApplicationLifetime lifetime;
        private readonly CancellationToken cancellationToken;

        public TestGrpcToGraphqlHostedService(
            ILogger<TestGrpcToGraphqlHostedService> logger,
            IHostApplicationLifetime lifetime)
        {
            this.logger = logger;
            this.lifetime = lifetime;
            this.cancellationToken = lifetime.ApplicationStopping;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Test gRPC to GraphQL Service starting.");

            // wait until the host is ready to start background work
            this.lifetime.ApplicationStarted.Register(() =>
            {
                this.logger.LogInformation("Test gRPC to GraphQL Service running.");

                Task.Run(async () =>
                {
                    try
                    {
                        await ProcessTest(cancellationToken);

                    }
                    catch (Exception error)
                    {
                        this.logger.LogError(error, error.ToString());
                    }
                }, cancellationToken);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Test gRPC to GraphQL Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {

        }

        private async Task ProcessTest(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Creating client to GraphQL API");

            // The port number(5001) must match the port of the gRPC server.
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            //var cl = new GraphqlGrpcService.GraphqlGrpcServiceClient(channel);
            //var client = new GraphqlService. .GraphqlServiceClient(channel);

            //this.logger.LogInformation("Client created");
            //this.logger.LogInformation($"Press space for continue...");
            //Console.ReadKey();

            //var stopWatch = new Stopwatch();
            //stopWatch.Start();
            //var pong = await client.PingAsync(new Empty());
            //stopWatch.Stop();
            //this.logger.LogInformation($"GraphqlService.PingAsync: {pong} (time: {stopWatch.ElapsedMilliseconds}ms)");
            //stopWatch.Reset();
            //stopWatch.Start();
            //var pong2 = await client.PingAsync(new Empty());
            //stopWatch.Stop();
            //this.logger.LogInformation($"GraphqlService.PingAsync: {pong2} (time: {stopWatch.ElapsedMilliseconds}ms)");
            //this.logger.LogInformation("Press any other key to continue.");
            //Console.ReadKey();

            //var query = @"
            //{
            //      greetings {
            //        hello
            //      }
            //}";
            //var request = new Request
            //{
            //    Query = query,
            //    Variables = new Struct()
            //    {
            //        Fields =
            //        {
            //            { "param1" , Value.ForNumber(1) },
            //            { "param2" , Value.ForString("test") }
            //        }

            //    },
            //    OperationName = "test"
            //};

            //stopWatch.Reset();
            //stopWatch.Start();

            //using (var call = client.Execute(request: request, headers: new Metadata { new Metadata.Entry("client-name", typeof(Program).Namespace) }))
            //{
            //    await foreach (var message in call.ResponseStream.ReadAllAsync())
            //    {
            //        this.logger.LogInformation();
            //        this.logger.LogInformation($"Result:");
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        this.logger.LogInformation(message.ToString());
            //        this.logger.LogInformation($"Request time: {stopWatch.ElapsedMilliseconds}ms");
            //        Console.ResetColor();
            //    }
            //}

            //Console.ForegroundColor = ConsoleColor.Red;
            //this.logger.LogInformation("Disconnected.");
            //Console.ResetColor();
            //this.logger.LogInformation("Press any key to exit...");
            //Console.ReadKey();
        }
    }
}
