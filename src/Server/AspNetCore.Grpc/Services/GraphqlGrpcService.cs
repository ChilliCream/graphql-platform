using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HotChocolate.Execution;
using Microsoft.Extensions.Logging;

namespace HotChocolate.AspNetCore.Grpc
{
    /// <inheritdoc/>
    public class GraphqlGrpcService
        : GraphqlService.GraphqlServiceBase
    {
        private readonly ILogger<GraphqlGrpcService> logger;
        private readonly IQueryExecutor queryExecutor;

        /// <inheritdoc/>
        public GraphqlGrpcService(
            ILogger<GraphqlGrpcService> logger,
            IQueryExecutor queryExecutor)
        {
            this.logger = logger;
            this.queryExecutor = queryExecutor;
        }

        /// <inheritdoc/>
        public override async Task Execute(
            Request request,
            IServerStreamWriter<Response> responseStream,
            ServerCallContext context)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (responseStream is null) throw new ArgumentNullException(nameof(responseStream));
            if (context is null) throw new ArgumentNullException(nameof(context));

            logger.LogDebug($"Executing GraphlQL request: {Environment.NewLine}{request}");

            // TODO: Get data from HTTP context - example cert etc.
            //var httpContext = context.GetHttpContext();
            //var clientCertificate = httpContext.Connection.ClientCertificate;

            var result = await this.queryExecutor.ExecuteAsync(request.Query, context.CancellationToken).ConfigureAwait(false);
            var response = result.ToGrpcResponse();
            await responseStream.WriteAsync(response).ConfigureAwait(false);

            logger.LogDebug($"Executed GraphlQL request.");
        }

        /// <inheritdoc/>
        public override Task<Empty> Ping(
            Empty request,
            ServerCallContext context)
                => Task.FromResult(new Empty());

    }
}
