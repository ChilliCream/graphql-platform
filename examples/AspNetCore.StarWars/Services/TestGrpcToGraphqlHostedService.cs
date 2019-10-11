using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using HotChocolate.AspNetCore.Grpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StarWars
{
    /// <summary>
    /// Test the gRPC client with the gRPC GraphQL service
    /// </summary>
    public class TestGrpcToGraphqlHostedService
        : IHostedService
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

                this.cancellationToken.ThrowIfCancellationRequested();

                Task.Run(async () =>
                {
                    try
                    {
                        await ProcessTest(this.cancellationToken);

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


        private async Task ProcessTest(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Creating client to GraphQL API");

            // The port number(5001) must match the port of the gRPC server.
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new GraphqlService.GraphqlServiceClient(channel);
            this.logger.LogDebug("Client created");

            var query = @"
            query TestQuery {
              Luke: character(characterIds: ""1000"") {
              ...CharacterFragment
              }

              R2D2: character(characterIds: ""2001"") {
                ...CharacterFragment
              }
            }

            fragment CharacterFragment on  Character {
                id
                __typename
                name
            }
            ";

            var request = new QueryRequest
            {
                Query = query,
                Variables = new Struct
                {
                    Fields =
                    {
                        { "param1" , Value.ForNumber(1) },
                        { "param2" , Value.ForString("test") }
                    }

                },
                OperationName = "TestQuery"
            };

            using var call = client.Query(
                request: request,
                headers: new Metadata
                {
                    new Metadata.Entry("client-name", typeof(Program).Namespace),
                    new Metadata.Entry("authentication", "<bearer token>")
                });

            await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                this.logger.LogInformation($"Query Result:{Environment.NewLine}{message}");
            }

            var queryBatchRequest = new QueryBatchRequest
            {
                Operation =
                {
                    request
                }
            };

            using var callBatchQuery = client.QueryBatch(
                request: queryBatchRequest,
                headers: new Metadata
                {
                    new Metadata.Entry("client-name", typeof(Program).Namespace),
                    new Metadata.Entry("authentication", "<bearer token>")
                });

            await foreach (var message in callBatchQuery.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                this.logger.LogInformation($"QueryBatch Result:{Environment.NewLine}{message}");
            }
        }
    }
}
