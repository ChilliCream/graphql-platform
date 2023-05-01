namespace HotChocolate.Types;

internal sealed class ErrorMiddleware
{
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
            List<Exception>? unhandledErrors = null;

            foreach (var exception in ex.InnerExceptions)
            {
                var handled = false;

                foreach (var createError in _errorHandlers)
                {
                    if (createError(exception) is { } error)
                    {
                        errors.Add(error);
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                {
                    unhandledErrors ??= new List<Exception>();
                    unhandledErrors.Add(exception);
                }
            }

            if (errors.Count == 0)
            {
                throw;
            }

            // if we have some errors that we could not handle
            // we will report them as GraphQL errors.
            if(unhandledErrors.Count > 0)
            {
                foreach (var unhandledError in unhandledErrors)
                {
                    context.ReportError(unhandledError);
                }
            }

            context.SetScopedState(ErrorContextDataKeys.Errors, errors);
            context.Result = MarkerObjects.ErrorObject;
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
            context.Result = MarkerObjects.ErrorObject;
        }
    }
}
