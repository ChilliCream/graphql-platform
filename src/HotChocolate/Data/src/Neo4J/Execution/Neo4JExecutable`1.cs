using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Driver;

namespace HotChocolate.Data.Neo4J.Execution
{
    public abstract class Neo4JExecutable<T>
        : INeo4JExecutable
        , IExecutable<T>
    {
        /// <inheritdoc />
        public object Source { get; }

        /// <summary>
        /// The filter definition that was set by <see cref="WithFiltering"/>
        /// </summary>
        protected Neo4JFilterDefinition? Filters { get; private set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        protected Neo4JSortDefinition? Sorting { get; private set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        protected Neo4JProjectionDefinition? Projection { get; private set; }

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

        public abstract ValueTask<IList> ToListAsync(CancellationToken cancellationToken);
        public abstract ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);
        public abstract ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken);
        public abstract string Print();
    }
}
