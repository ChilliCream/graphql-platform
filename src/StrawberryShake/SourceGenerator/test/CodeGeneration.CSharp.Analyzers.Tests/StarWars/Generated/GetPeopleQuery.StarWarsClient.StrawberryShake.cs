﻿#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleQuery
    {
        private readonly global::StrawberryShake.IOperationExecutor<IGetPeople> _operationExecutor;

        public GetPeopleQuery(global::StrawberryShake.IOperationExecutor<IGetPeople> operationExecutor)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
        }

        public async global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<IGetPeople>> ExecuteAsync(global::System.Threading.CancellationToken cancellationToken = default)
        {
            var request = CreateRequest();
            
            return await _operationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<IGetPeople>> Watch(global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest();
            return _operationExecutor.Watch(request, strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest()
        {

            return new global::StrawberryShake.OperationRequest(
                "GetPeople",
                GetPeopleQueryDocument.Instance);
        }
    }
}
