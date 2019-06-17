using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Client.Core.Deserializers;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// An auto-paging GraphQL query.
    /// </summary>
    /// <typeparam name="TResult">The query result type.</typeparam>
    /// <remarks>
    /// A paged query consists of a master query and any number of sub-queries (which may
    /// themselves be paged queries). When the full set of results cannot be returned by
    /// the master query, the sub-queries are run on order to page in the additional data.
    /// </remarks>
    public class PagedQuery<TResult> : ICompiledQuery<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PagedQuery{TResult}"/> class.
        /// </summary>
        /// <param name="masterQuery">The master query.</param>
        /// <param name="subqueries">The sub-queries.</param>
        public PagedQuery(
            SimpleQuery<TResult> masterQuery,
            IEnumerable<ISubquery> subqueries)
        {
            MasterQuery = masterQuery;
            Subqueries = subqueries.ToList();
        }

        /// <summary>
        /// Gets a value indicating whether the query is a mutation.
        /// </summary>
        public bool IsMutation => MasterQuery.IsMutation;

        /// <summary>
        /// Gets the master query.
        /// </summary>
        public SimpleQuery<TResult> MasterQuery { get; }

        /// <summary>
        /// Gets the sub-queries.
        /// </summary>
        public IReadOnlyList<ISubquery> Subqueries { get; }

        /// <summary>
        /// Returns an <see cref="IQueryRunner{TResult}"/> which can be used to run the query on a connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="variables">The query variables.</param>
        /// <returns>A query runner.</returns>
        public IQueryRunner<TResult> Start(IConnection connection, IDictionary<string, object> variables)
        {
            return new Runner(this, connection, variables);
        }

        /// <inheritdoc/>
        public override string ToString() => ToString(2);

        /// <inheritdoc/>
        public string ToString(int indentation) => MasterQuery.ToString(indentation);

        /// <inheritdoc/>
        IQueryRunner ICompiledQuery.Start(IConnection connection, IDictionary<string, object> variables)
        {
            return Start(connection, variables);
        }

        protected class Runner : IQueryRunner<TResult>, ISubqueryRunner
        {
            readonly PagedQuery<TResult> owner;
            readonly IConnection connection;
            readonly ResponseDeserializer deserializer = new ResponseDeserializer();
            Stack<IQueryRunner> subqueryRunners;
            Dictionary<ISubquery, List<Action<object>>> subqueryResultSinks;

            public Runner(
                PagedQuery<TResult> owner,
                IConnection connection,
                IDictionary<string, object> variables)
            {
                this.owner = owner;
                this.connection = connection;
                this.Variables = variables;
            }

            /// <inheritdoc />
            public TResult Result { get; private set; }

            /// <inheritdoc />
            object IQueryRunner.Result => Result;

            protected IDictionary<string, object> Variables { get; }

            /// <inheritdoc />
            public virtual async Task<bool> RunPage(CancellationToken cancellationToken = default)
            {
                if (subqueryRunners == null)
                {
                    subqueryRunners = new Stack<IQueryRunner>();
                    subqueryResultSinks = new Dictionary<ISubquery, List<Action<object>>>();

                    // This is the first run, so run the master page.
                    var master = owner.MasterQuery;
                    var data = await connection.Run(master.GetPayload(Variables), cancellationToken).ConfigureAwait(false);
                    var json = deserializer.Deserialize(data);

                    json.AddAnnotation(this);
                    Result = deserializer.Deserialize(master.ResultBuilder, json);

                    // Look through each subquery for any results that have a next page.
                    foreach (var subquery in owner.Subqueries)
                    {
                        var pageInfos = subquery.ParentPageInfo(json).ToList();
                        var parentIds = subquery.ParentIds(json).ToList();

                        if (subqueryResultSinks.TryGetValue(subquery, out var sinks))
                        {
                            for (var i = 0; i < pageInfos.Count; ++i)
                            {
                                var pageInfo = pageInfos[i];

                                if ((bool)pageInfo["hasNextPage"] == true)
                                {
                                    var id = parentIds[i].ToString();
                                    var after = (string)pageInfo["endCursor"];
                                    var runner = subquery.Start(connection, id, after, Variables, sinks[i]);
                                    subqueryRunners.Push(runner);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Get the next subquery runner.
                    var runner = subqueryRunners.Peek();

                    // Run its next page and pop it from the active runners if finished.
                    if (!await runner.RunPage(cancellationToken).ConfigureAwait(false))
                    {
                        subqueryRunners.Pop();
                    }
                }

                return subqueryRunners.Count > 0;
            }

            /// <inheritdoc />
            public void SetQueryResultSink(ISubquery query, Action<object> add)
            {
                if (!subqueryResultSinks.TryGetValue(query, out var value))
                {
                    value = new List<Action<object>>();
                    subqueryResultSinks.Add(query, value);
                }

                value.Add(add);
            }
        }
    }
}
