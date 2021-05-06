using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Neo4J.Sorting;
using HotChocolate.Language;
using Neo4j.Driver;

#nullable enable

namespace HotChocolate.Data.Neo4J.Execution
{
    public class Neo4JExecutable<T> : INeo4JExecutable, IExecutable<T>
    {
        private readonly IAsyncSession _session;

        private static Node Node => Cypher.NamedNode(typeof(T).Name);

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

        /// <summary>
        /// The skip paging definition
        /// </summary>
        private int? Skip { get; set; }

        /// <summary>
        /// The limit paging definition
        /// </summary>
        private int? Limit { get; set; }

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

        public Neo4JExecutable<T> WithSkip(int skip)
        {
            Skip = skip;
            return this;
        }

        public Neo4JExecutable<T> WithLimit(int limit)
        {
            Limit = limit;
            return this;
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
            StatementBuilder statement = Cypher.Match(Node).Return(Node);

            if (Filters is not null)
            {
                statement.Match(new Where(Filters), Node);
            }

            if (Projection is not null)
            {
                statement.Return(Node.Project(Projection));
            }

            if (Sorting is null) return statement;

            var sorts = new List<SortItem>();

            foreach (Neo4JSortDefinition sort in Sorting)
            {
                SortItem? sortItem = Cypher.Sort(Node.Property(sort.Field));
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

            if (Limit is not null) statement.Limit((int)Limit);
            if (Skip is not null) statement.Limit((int)Skip);

            return statement;
        }
    }
}
