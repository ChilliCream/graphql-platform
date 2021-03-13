using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Neo4J.Sorting;
using HotChocolate.Language;
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
        private CompoundCondition? Filters { get; set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        private Neo4JSortDefinition[]? Sorting { get; set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        private object[]? Projection { get; set; }

        private object Paging { get; set; }

        public object? Source { get; }

        public Neo4JExecutable(IAsyncSession session)
        {
            _session = session;
        }

        public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            IResultCursor cursor = await _session.RunAsync(Pipeline().Build());
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
            return Pipeline().Build() ?? "";
        }

        public INeo4JExecutable WithFiltering(CompoundCondition filters)
        {
            Filters = filters;
            return this;
        }

        public INeo4JExecutable WithSorting(Neo4JSortDefinition[] sorting)
        {
            Sorting = sorting;
            return this;
        }

        public INeo4JExecutable WithProjection(object[] projection)
        {
            Projection = projection;
            return this;
        }

        public StatementBuilder Pipeline()
        {
            Node node = Cypher.NamedNode(typeof(T).Name);

            StatementBuilder statement = Cypher.Match(node).Return(node);

            if (Filters is not null)
            {
                statement.Match(new Where(Filters), node);
            }

            if (Projection is not null)
            {
                statement.Return(node.Project(Projection));
            }

            if (Sorting is null) return statement;

            var sorts = new List<SortItem>();

            foreach (Neo4JSortDefinition sort in Sorting)
            {
                SortItem? sortItem = Cypher.Sort(node.Property(sort.Field));
                if (sort.Direction == SortDirection.Ascending)
                {
                    sorts.Push(sortItem.Ascending());
                }
                else if (sort.Direction == SortDirection.Descending)
                {
                    sorts.Push(sortItem.Descending());
                }

            }
            statement.OrderBy(sorts);

            return statement;
        }
    }
}
