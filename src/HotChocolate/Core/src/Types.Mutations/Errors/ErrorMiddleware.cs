namespace HotChocolate.Types;

internal sealed class ErrorMiddleware
{
    public static readonly object ErrorObject = new();

    private readonly FieldDelegate _next;
    private readonly IReadOnlyList<CreateError> _errorHandlers;

    public ErrorMiddleware(FieldDelegate next, IReadOnlyList<CreateError> errorHandlers)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _errorHandlers = errorHandlers ??
            throw new ArgumentNullException(nameof(errorHandlers));
    }

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (GraphQLException)
        {
            throw;
        }
        catch (AggregateException ex)
        {
            var errors = new List<object>();

            foreach (var exception in ex.InnerExceptions)
            {
                foreach (var createError in _errorHandlers)
                {
                    if (createError(exception) is { } error)
                    {
                        errors.Add(error);
                        break;
                    }
                }
            }

            if (errors.Count == 0)
            {
                throw;
            }

            context.SetScopedState(ErrorContextDataKeys.Errors, errors);
            context.Result = ErrorObject;
        }
        catch (Exception ex)
        {
            object? error = null;

            foreach (var createError in _errorHandlers)
            {
                if (createError(ex) is { } e)
                {
                    error = e;
                    break;
                }
            }

            if (error is null)
            {
                throw;
            }

            context.SetScopedState(ErrorContextDataKeys.Errors, new[] { error });
            context.Result = ErrorObject;
        }
    }
}
