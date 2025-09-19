namespace HotChocolate.Execution.Errors;

internal class FuncErrorFilterWrapper
    : IErrorFilter
{
    private readonly Func<IError, IError> _errorFilter;

    public FuncErrorFilterWrapper(
        Func<IError, IError> errorFilter)
    {
        _errorFilter = errorFilter
            ?? throw new ArgumentNullException(nameof(errorFilter));
    }

    public IError OnError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return _errorFilter(error);
    }
}
