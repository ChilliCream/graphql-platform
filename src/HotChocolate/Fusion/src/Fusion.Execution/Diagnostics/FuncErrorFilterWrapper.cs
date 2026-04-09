using HotChocolate.Execution;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class FuncErrorFilterWrapper(Func<IError, IError> errorFilter) : IErrorFilter
{
    private readonly Func<IError, IError> _errorFilter = errorFilter
        ?? throw new ArgumentNullException(nameof(errorFilter));

    public IError OnError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return _errorFilter(error);
    }
}
