using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using HotChocolate.Client.Core.Builders;
using HotChocolate.Client.Core.Deserializers;
using HotChocolate.Client.Core.Serializers;
using HotChocolate.Client.Core.Syntax;
using HotChocolate.Language;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// A simple GraphQL query with no auto-paging.
    /// </summary>
    /// <typeparam name="TResult">The query result type.</typeparam>
    public class SimpleQuery<TResult> : ICompiledQuery<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleQuery{TResult}"/> class.
        /// </summary>
        /// <param name="operationDefinition">The GraphQL operation definition.</param>
        /// <param name="resultBuilder">
        /// A function which transforms JSON data into the final result.
        /// </param>
        public SimpleQuery(
            OperationDefinitionNode operationDefinition,
            Expression<Func<JObject, TResult>> resultBuilder)
        {
            var serializer = new QuerySerializer();
            OperationDefinition = operationDefinition;
            Query = serializer.Serialize(operationDefinition);
            ResultBuilder = ExpressionCompiler.Compile(resultBuilder);
        }

        /// <summary>
        /// Gets a value indicating whether the query is a mutation.
        /// </summary>
        public bool IsMutation => OperationDefinition.Type == OperationType.Mutation;

        /// <summary>
        /// Gets the GraphQL operation definition.
        /// </summary>
        public OperationDefinitionNode OperationDefinition { get; }

        /// <summary>
        /// Gets the GraphQL query string.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Gets a function which transforms JSON data into the final result.
        /// </summary>
        public Func<JObject, TResult> ResultBuilder { get; }

        /// <inheritdoc/>
        public override string ToString() => ToString(true);

        /// <inheritdoc/>
        public string ToString(bool useIndentation)
        {
            return QuerySyntaxSerializer.Serialize(
                new DocumentNode(OperationDefinition),
                useIndentation);
        }

        /// <summary>
        /// Gets a payload string consisting of the <see cref="Query"/> and any variables.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The payload string.</returns>
        public string GetPayload(IDictionary<string, object> variables)
        {
            var payload = new
            {
                Query,
                Variables = variables,
            };

            return JsonConvert.SerializeObject(
                payload,
                Formatting.None,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }

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
        IQueryRunner ICompiledQuery.Start(IConnection connection, IDictionary<string, object> variables)
        {
            return Start(connection, variables);
        }

        class Runner : IQueryRunner<TResult>
        {
            readonly SimpleQuery<TResult> parent;
            readonly IConnection connection;
            readonly IDictionary<string, object> variables;

            public Runner(
                SimpleQuery<TResult> parent,
                IConnection connection,
                IDictionary<string, object> variables)
            {
                this.parent = parent;
                this.connection = connection;
                this.variables = variables;
            }

            /// <inheritdoc />
            public TResult Result { get; private set; }

            /// <inheritdoc />
            object IQueryRunner.Result => Result;

            /// <inheritdoc />
            public async Task<bool> RunPage(CancellationToken cancellationToken = default)
            {
                var deserializer = new ResponseDeserializer();
                var data = await connection.Run(parent.GetPayload(variables), cancellationToken).ConfigureAwait(false);
                Result = deserializer.Deserialize(parent.ResultBuilder, data);
                return false;
            }
        }
    }
}
