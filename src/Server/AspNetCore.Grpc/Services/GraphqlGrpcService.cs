using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HotChocolate.Execution;
using HotChocolate.Execution.Batching;
using Microsoft.Extensions.Logging;

namespace HotChocolate.AspNetCore.Grpc
{
    /// <inheritdoc/>
    public class GraphqlGrpcService
        : GraphqlService.GraphqlServiceBase
    {
        private readonly ILogger<GraphqlGrpcService> logger;
        private readonly IQueryExecutor queryExecutor;
        private readonly IBatchQueryExecutor batchQueryExecutor;

        /// <inheritdoc/>
        public GraphqlGrpcService(
            ILogger<GraphqlGrpcService> logger,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchQueryExecutor)
        {
            this.logger = logger;
            this.queryExecutor = queryExecutor;
            this.batchQueryExecutor = batchQueryExecutor;
        }

        /// <inheritdoc/>
        public override async Task Query(
            QueryRequest request,
            IServerStreamWriter<QueryResponse> responseStream,
            ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (responseStream is null)
                throw new ArgumentNullException(nameof(responseStream));
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            // TODO: Get data from HTTP context - example cert etc.
            //var httpContext = context.GetHttpContext();
            //var clientCertificate = httpContext.Connection.ClientCertificate;

            var result = await this.queryExecutor
                .ExecuteAsync(request.Query, context.CancellationToken).ConfigureAwait(false);
            var response = result.ToGrpcQueryResponse();

            await responseStream.WriteAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task QueryBatch(
            QueryBatchRequest request,
            IServerStreamWriter<QueryResponse> responseStream,
            ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (responseStream is null)
                throw new ArgumentNullException(nameof(responseStream));
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var batch = request.Operation.Select(
                    operation => QueryRequestBuilder.New()
                        .SetQuery(operation.Query)
                        .Create())
                .ToImmutableList();

            //var batch = new List<IReadOnlyQueryRequest>
            //{
            //    QueryRequestBuilder.New()
            //        .SetQuery(
            //            @"
            //            query getHero {
            //                hero(episode: EMPIRE) {
            //                    id @export
            //                }
            //            }")
            //        .Create(),
            //    QueryRequestBuilder.New()
            //        .SetQuery(
            //            @"
            //            query getHuman {
            //                human(id: $id) {
            //                    name
            //                }
            //            }")
            //        .Create()
            //};

            var stream = await this.batchQueryExecutor
                .ExecuteAsync(batch, context.CancellationToken).ConfigureAwait(false);

            while (!stream.IsCompleted)
            {
                var result = await stream.ReadAsync(context.CancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    this.logger.LogInformation($"Result:{Environment.NewLine}{result}");
                    var response = result.ToGrpcQueryResponse();
                    await responseStream.WriteAsync(response).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public override async Task Mutation(
            MutationRequest request,
            IServerStreamWriter<MutationResponse> responseStream,
            ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (responseStream is null)
                throw new ArgumentNullException(nameof(responseStream));
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            throw new RpcException(new Status(StatusCode.Unimplemented, nameof(Mutation)));
        }

        /// <inheritdoc/>
        public override async Task Subscription(
            SubscriptionRequest request,
            IServerStreamWriter<SubscriptionResponse> responseStream,
            ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (responseStream is null)
                throw new ArgumentNullException(nameof(responseStream));
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            throw new RpcException(new Status(StatusCode.Unimplemented, nameof(Subscription)));
        }
    }
}
