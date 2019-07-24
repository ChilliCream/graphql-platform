namespace HotChocolate.Client
{
    public interface IReadOnlyQueryRequest
    {

    }

    public interface IExecutionResult
    {

    }


    public interface IReadOnlyQueryResult
        : IExecutionResult
    {

    }

    public interface IConnection
    {
        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken);
    }

    public interface IQueryableItem
    {
        Expression Expression { get; }
    }

    public interface IQueryableList
        : IQueryableItem
    {

    }

    public interface IQueryCompiler
    {
        ICompiledQuery<T> Compile(IQueryableItem queryable);
    }

    public interface IStreamCompiler
    {
        ICompiledQuery<T> Compile(IQueryableItem queryable);
    }

    public interface ICompiledQuery<T>
    {
        Task<T> ExecuteAsync(CancellationToken cancellationToken);
    }

    // batch => response stream
    // sub => res stream

}
