using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlKata;
using SqlKata.Execution;

namespace HotChocolate.Data.SqlKata
{
    /// <summary>
    /// Is the base class for a executable for the SqlKata.
    /// </summary>
    public class SqlKataExecutable<T>
        : IExecutable<T>
        , ISqlKataExecutable
    {
        private QueryFactory _queryFactory;

        public SqlKataExecutable()
        {
            Source = new Query();
        }

        public SqlKataExecutable(Query source)
        {
            Source = source;
        }

        /// <summary>
        /// The filter definition that was set by <see cref="WithFiltering"/>
        /// </summary>
        protected Query? Filters { get; private set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        protected Query? Sorting { get; private set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        protected Query? Projection { get; private set; }

        /// <inheritdoc />
        public ISqlKataExecutable WithFiltering(Query filters)
        {
            Filters = filters;
            return this;
        }

        /// <inheritdoc />
        public ISqlKataExecutable WithQueryFactory(QueryFactory queryFactory)
        {
            _queryFactory = queryFactory;
            return this;
        }

        /// <inheritdoc />
        public ISqlKataExecutable WithSorting(Query sorting)
        {
            Sorting = sorting;
            return this;
        }

        /// <inheritdoc />
        public ISqlKataExecutable WithProjection(Query projection)
        {
            Projection = projection;
            return this;
        }

        /// <inheritdoc />
        public object Source { get; }

        /// <inheritdoc />
        public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            IEnumerable<T> result = await _queryFactory
                .FromQuery(Filters)
                .GetAsync<T>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result.ToArray();
        }

        /// <inheritdoc />
        public async ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            return await _queryFactory
                .FromQuery(Filters)
                .FirstOrDefaultAsync<T>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException();
            /*
            return await _queryFactory
                .FromQuery(Filters)
                .FirstOrDefaultAsync<T>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            */
        }

        /// <inheritdoc />
        public string Print()
        {
            return _queryFactory.Compiler.Compile(Filters).Sql;
        }
    }
}
