using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Filtering;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Neo4J.Projections;
using HotChocolate.Data.Neo4J.Sorting;
using Neo4j.Driver;
using ServiceStack;

#nullable enable

namespace HotChocolate.Data.Neo4J.Execution
{
    public class Neo4JExecutable<T> : INeo4JExecutable, IExecutable<T>
    {
        //private readonly CypherQuery _cypher;
        private readonly IAsyncSession _session;

        /// <summary>
        /// The filter definition that was set by <see cref="WithFiltering"/>
        /// </summary>
        private Neo4JFilterDefinition? Filters { get; set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        private Neo4JSortDefinition? Sorting { get; set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        private Neo4JProjectionDefinition? Projection { get; set; }

        private object Paging { get; set; }

        public object? Source { get; }

        public Neo4JExecutable(IAsyncSession session)
        {
            _session = session;
        }

        public Neo4JExecutable()
        {
        }


        public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            Node node = Cypher
                .Node(typeof(T).Name)
                .Named(typeof(T).Name.ToCamelCase());

            StatementBuilder statement = Cypher.Match(node);

            // IResultCursor cursor = await _session.RunAsync(@"
            //     MATCH (business:Business)
            //     RETURN business { .name, .city, .state, reviews: [(business)<-[:REVIEWS]-(reviews) | reviews {.rating, .text}] }
            // ").ConfigureAwait(false);

            if (Filters is not null)
            {
                statement.Where(Filters.Condition);
            }

            if (Projection is not null)
            {
                statement.Return(Projection.Expressions);
            }

            if (Sorting is not null)
            {

            }

            IResultCursor cursor = await _session.RunAsync(statement.Build());
            return await cursor.MapAsync<T>().ConfigureAwait(false);
        }

        public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string Print()
        {
            return "";
        }

        public INeo4JExecutable WithFiltering(Neo4JFilterDefinition filters)
        {
            Filters = filters;
            return this;
        }

        public INeo4JExecutable WithSorting(Neo4JSortDefinition sorting)
        {
            Sorting = sorting;
            return this;
        }

        public INeo4JExecutable WithProjection(Neo4JProjectionDefinition projection)
        {
            Projection = projection;
            return this;
        }
    }
}
