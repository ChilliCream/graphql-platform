namespace HotChocolate.Types;

internal sealed class QueryResultMiddleware(FieldDelegate next, IReadOnlyList<CreateError> errorHandlers)
{
    private readonly FieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));

    private readonly IReadOnlyList<CreateError> _errorHandlers = errorHandlers ??
        throw new ArgumentNullException(nameof(errorHandlers));

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);

            // we need to unwrap field results.
            if (context.Result is IFieldResult fieldResult)
            {
                if (fieldResult.IsSuccess)
                {
                    context.Result = fieldResult.Value;
                }
                else
                {
                    // TODO : this is not good ... it should be clear how errors have to be unwrapped
                    context.Result = ((object[])fieldResult.Value!)[0];
                }
            }
        }
        catch (GraphQLException)
        {
            throw;
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

            context.Result = error;
        }
    }
}
