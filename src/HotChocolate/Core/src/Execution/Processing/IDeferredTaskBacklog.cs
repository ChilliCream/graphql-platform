namespace HotChocolate.Execution.Processing
{
    internal interface IDeferredTaskBacklog
    {
        bool IsEmpty { get; }

        IDeferredExecutionTask Take();

        void Register(IDeferredExecutionTask deferredTask);

        void Clear();
    }
}
