using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Filtering;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Neo4J.Projections;
using HotChocolate.Data.Neo4J.Sorting;

namespace HotChocolate.Data.Neo4J.Execution
{
    public class Neo4JExecutable<T> : INeo4JExecutable where T : class
    {
        private readonly CypherQuery<T> _cypher;

        /// <inheritdoc />
        public Neo4JExecutable(CypherQuery<T> cypher)
        {
            _cypher = cypher;
        }
        // readonly Cypher _builder;
        public object Source { get; }

        public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
            throw new System.NotImplementedException();
        }

        public INeo4JExecutable WithFiltering(Neo4JFilterDefinition filters)
        {
            throw new NotImplementedException();
        }

        public INeo4JExecutable WithSorting(Neo4JSortDefinition sorting)
        {
            throw new NotImplementedException();
        }

        public INeo4JExecutable WithProjection(Neo4JProjectionDefinition projection)
        {
            throw new NotImplementedException();
        }
    }
}
