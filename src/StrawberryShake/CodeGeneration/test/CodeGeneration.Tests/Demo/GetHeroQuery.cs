namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroQuery
    {
        private readonly global::StrawberryShake.IOperationExecutor<IGetHero> _operationExecutor;

        public GetHeroQuery(global::StrawberryShake.IOperationExecutor<IGetHero> operationExecutor)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
        }

        public async global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<IGetHero>> Execute(global::System.Threading.CancellationToken cancellationToken = default)
        {
            var request = CreateRequest();
            
            return await _operationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<IGetHero>> Watch(global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest();
            return _operationExecutor.Watch(request, strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest()
        {

            return new global::StrawberryShake.OperationRequest(
                "IGetHero",
                GetHeroQueryDocument.Instance
            );
        }
    }
}
