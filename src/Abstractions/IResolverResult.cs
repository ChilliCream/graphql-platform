namespace HotChocolate
{
    public interface IResolverResult
    {
        string ErrorMessage { get; }

        bool IsError { get; }

        object Value { get; }
    }

    public interface IResolverResult<TValue>
        : IResolverResult
    {
        new TValue Value { get; }
    }
}
