using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using HotChocolate.Client.Core.Builders;
using HotChocolate.Client.Core.Deserializers;
using HotChocolate.Client.Core.Syntax;
using HotChocolate.Language;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// A sub-query of a <see cref="PagedQuery{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The query result type.</typeparam>
    public class SimpleSubquery<TResult> : SimpleQuery<TResult>, ISubquery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleQuery{TResult}"/> class.
        /// </summary>
        /// <param name="operationDefinition">The GraphQL operation definition.</param>
        /// <param name="resultBuilder">
        /// A function which transforms JSON data into the final result.
        /// </param>
        /// <param name="parentIds">
        /// A function which given the data from the master query, returns the IDs of the
        /// entities which can be auto-paged.
        /// </param>
        /// <param name="pageInfo">
        /// A function which given the data from the sub-query, returns the paging info.
        /// </param>
        /// <param name="parentPageInfo">
        /// A function which given the data from the master query, returns the paging info
        /// for all entities which can be auto-paged.
        /// </param>
        public SimpleSubquery(
            OperationDefinitionNode operationDefinition,
            Expression<Func<JObject, TResult>> resultBuilder,
            Expression<Func<JObject, IEnumerable<JToken>>> parentIds,
            Expression<Func<JObject, JToken>> pageInfo,
            Expression<Func<JObject, IEnumerable<JToken>>> parentPageInfo)
            : base(operationDefinition, resultBuilder)
        {
            ParentIds = ExpressionCompiler.Compile(parentIds);
            PageInfo = ExpressionCompiler.Compile(pageInfo);
            ParentPageInfo = ExpressionCompiler.Compile(parentPageInfo);
        }

        /// <summary>
        /// Gets a function which given the data from the master query, returns the IDs of the
        /// entities which can be auto-paged.
        /// </summary>
        public Func<JObject, IEnumerable<JToken>> ParentIds { get; }

        /// <summary>
        /// Gets a function which given the data from the sub-query, returns the paging info.
        /// </summary>
        public Func<JObject, JToken> PageInfo { get; }

        /// <summary>
        /// Gets a function which given the data from the master query, returns the paging info
        /// for all entities which can be auto-paged.
        /// </summary>
        public Func<JObject, IEnumerable<JToken>> ParentPageInfo { get; }

        /// <inheritdoc/>
        public IQueryRunner Start(
            IConnection connection,
            string id,
            string after,
            IDictionary<string, object> variables,
            Action<object> addResult)
        {
            return new Runner(this, connection, id, after, variables, addResult);
        }

        internal static ISubquery Create(
            Type resultType,
            OperationDefinitionNode operationDefinition,
            Expression expression,
            Expression<Func<JObject, IEnumerable<JToken>>> parentIds,
            Expression<Func<JObject, JToken>> pageInfo,
            Expression<Func<JObject, IEnumerable<JToken>>> parentPageInfo)
        {
            var ctor = typeof(SimpleSubquery<>)
                .MakeGenericType(resultType)
                .GetTypeInfo()
                .DeclaredConstructors
                .Single();

            return (ISubquery)ctor.Invoke(new object[]
            {
                operationDefinition,
                expression,
                parentIds,
                pageInfo,
                parentPageInfo
            });
        }

        class Runner : IQueryRunner<TResult>
        {
            readonly SimpleSubquery<TResult> owner;
            readonly IConnection connection;
            readonly Dictionary<string, object> variables;
            readonly ResponseDeserializer deserializer = new ResponseDeserializer();
            readonly Action<object> addResult;

            public Runner(
               SimpleSubquery<TResult> owner,
               IConnection connection,
               string id,
               string after,
               IDictionary<string, object> variables,
               Action<object> addResult)
            {
                this.owner = owner;
                this.connection = connection;
                this.variables = variables?.ToDictionary(x => x.Key, x => x.Value) ??
                    new Dictionary<string, object>();
                this.variables["__id"] = id;
                this.variables["__after"] = after;
                this.addResult = addResult;
            }

            /// <inheritdoc />
            public TResult Result { get; private set; }

            /// <inheritdoc />
            object IQueryRunner.Result => Result;

            /// <inheritdoc />
            public async Task<bool> RunPage(CancellationToken cancellationToken = default)
            {
                var payload = owner.GetPayload(variables);
                var data = await connection.Run(payload, cancellationToken).ConfigureAwait(false);
                var json = deserializer.Deserialize(data);
                var pageInfo = owner.PageInfo(json);

                Result = owner.ResultBuilder(json);

                foreach (var i in (IList)Result)
                {
                    addResult(i);
                }

                if ((bool)pageInfo["hasNextPage"] == true)
                {
                    variables["__after"] = (string)pageInfo["endCursor"];
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
