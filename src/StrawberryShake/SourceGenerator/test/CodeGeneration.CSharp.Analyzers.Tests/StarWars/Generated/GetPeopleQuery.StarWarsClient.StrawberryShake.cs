// StrawberryShake.CodeGeneration.CSharp.Generators.OperationServiceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    /// <summary>
    /// Represents the operation service of the GetPeople GraphQL operation
    /// <code>
    /// query GetPeople {
    ///   people(order_by: { name: ASC }) {
    ///     __typename
    ///     nodes {
    ///       __typename
    ///       name
    ///       email
    ///       isOnline
    ///       lastSeen
    ///       ... on Person {
    ///         id
    ///       }
    ///     }
    ///   }
    /// }
    /// </code>
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleQuery
    {
        private readonly global::StrawberryShake.IOperationExecutor<IGetPeopleResult> _operationExecutor;

        public GetPeopleQuery(global::StrawberryShake.IOperationExecutor<IGetPeopleResult> operationExecutor)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
        }

        public async global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<IGetPeopleResult>> ExecuteAsync(global::System.Threading.CancellationToken cancellationToken = default)
        {
            var request = CreateRequest();

            return await _operationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<IGetPeopleResult>> Watch(global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest();
            return _operationExecutor.Watch(
                request,
                strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest()
        {

            return new global::StrawberryShake.OperationRequest(
                id: GetPeopleQueryDocument.Instance.Hash.Value,
                name: "GetPeople",
                document: GetPeopleQueryDocument.Instance,
                strategy: global::StrawberryShake.RequestStrategy.Default);
        }
    }
}
