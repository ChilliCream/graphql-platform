namespace StrawberryShake
{
    public interface IOperationStore
    {
        // IRequest / IOperationResult / [EntityId]

        void Set<T>(IOperationRequest operationRequest, IOperationResult<T> result) where T : class;

        IOperationResult<T>? Get<T>(IOperationRequest operationRequest) where T : class;

        IOperationObservable<T> Watch<T>(IOperationRequest operationRequest) where T : class;
    }
}
