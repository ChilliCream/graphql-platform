namespace StrawberryShake.Configuration
{
    public interface IOperationExecutionConfiguration
    {
        ExecutorKind Kind { get; }

        void Apply(IServiceConfiguration services, string schemaName);
    }
}
