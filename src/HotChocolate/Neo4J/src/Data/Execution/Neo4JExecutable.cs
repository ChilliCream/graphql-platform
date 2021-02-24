using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Filtering;
using HotChocolate.Data.Neo4J.Projections;
using HotChocolate.Data.Neo4J.Sorting;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Execution
{
    public class Neo4JExecutable<T> : INeo4JExecutable, IExecutable<T>
    {
        //private readonly CypherQuery _cypher;
        private readonly IAsyncSession _session;

        /// <summary>
        /// The filter definition that was set by <see cref="WithFiltering"/>
        /// </summary>
        protected Neo4JFilterDefinition Filters { get; private set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        protected Neo4JSortDefinition Sorting { get; private set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        protected Neo4JProjectionDefinition Projection { get; private set; }

        /// <inheritdoc />
        public Neo4JExecutable(IAsyncSession session)
        {
            _session = session;
        }

        public Neo4JExecutable()
        {
        }
        // readonly Cypher _builder;
        public object Source { get; }

        public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            IResultCursor cursor = await _session.RunAsync(@"
                MATCH (m:Movie)
                RETURN m
            ").ConfigureAwait(false);

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
