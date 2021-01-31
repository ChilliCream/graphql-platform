using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
public static class AsyncSessionExtensions
    {
        public static async Task<TEntity> GetNodeAsync<TEntity>(
            this IAsyncSession asyncSession,
            long nodeId) where TEntity : class
        {
            IDictionary<string, object> parameters = new CypherQueryParameters()
                .WithValue("p1", nodeId);

            IResultCursor resultCursor = await asyncSession.RunAsync(Constants.Statement.GetNode, parameters)
                .ConfigureAwait(false);

            return await resultCursor.MapSingleAsync<TEntity>().ConfigureAwait(false);
        }

        public static async Task<TEntity> GetNodeAsync<TEntity>(
            this IAsyncTransaction asyncTransaction,
            long nodeId) where TEntity : class
        {
            IDictionary<string, object> parameters = new CypherQueryParameters()
                .WithValue("p1", nodeId);

            IResultCursor resultCursor = await asyncTransaction.RunAsync(Constants.Statement.GetNode, parameters)
                .ConfigureAwait(false);

            return await resultCursor.MapSingleAsync<TEntity>().ConfigureAwait(false);
        }

        public static async Task<IResultCursor> SetNodeAsync<TEntity>(
            this IAsyncSession asyncSession,
            TEntity entity) where TEntity : class
        {
            var nodeId = EntityPropertyAccessor.GetNodeId(entity);
            if (nodeId == null)
                throw new InvalidOperationException(Constants.NodeIdUnspecifiedMessage);

            IDictionary<string, object> parameters = new CypherQueryParameters()
                .WithValue("p1", nodeId)
                .WithEntity("p2", entity);

            return await asyncSession.RunAsync(Constants.Statement.SetNode, parameters)
                .ConfigureAwait(false);
        }

        public static async Task<IResultCursor> SetNodeAsync<TEntity>(
            this IAsyncTransaction asyncTransaction,
            TEntity entity) where TEntity : class
        {
            var nodeId = EntityPropertyAccessor.GetNodeId(entity);
            if (nodeId == null)
                throw new InvalidOperationException(Constants.NodeIdUnspecifiedMessage);

            var parameters = new CypherQueryParameters()
                .WithValue("p1", nodeId)
                .WithEntity("p2", entity);

            return await asyncTransaction.RunAsync(Constants.Statement.SetNode, parameters)
                .ConfigureAwait(false);
        }
    }
}
