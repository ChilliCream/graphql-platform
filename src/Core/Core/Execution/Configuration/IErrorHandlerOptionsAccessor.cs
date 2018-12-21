namespace HotChocolate.Execution.Configuration
{
    public interface IErrorHandlerOptionsAccessor
    {
        bool IncludeExceptionDetails { get; }
    }
}
