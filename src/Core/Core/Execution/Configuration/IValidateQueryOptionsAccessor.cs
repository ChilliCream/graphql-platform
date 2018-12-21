namespace HotChocolate.Execution.Configuration
{
    public interface IValidateQueryOptionsAccessor
    {
        int? MaxExecutionDepth { get; }
    }
}
