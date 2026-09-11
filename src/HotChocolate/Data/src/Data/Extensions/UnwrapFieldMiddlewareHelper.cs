using HotChocolate.Data;
using HotChocolate.Resolvers;

namespace HotChocolate.Types;

internal static class UnwrapFieldMiddlewareHelper
{
    internal static BatchFieldMiddleware CreateBatchDataMiddleware(IQueryBuilder builder)
        => next => async contexts =>
        {
            foreach (var context in contexts)
            {
                if (HasErrorResult(context))
                {
                    continue;
                }

                try
                {
                    builder.Prepare(context);
                }
                catch (Exception ex)
                {
                    context.ReportError(ex);
                    context.Result = null;
                }
            }

            await next(contexts).ConfigureAwait(false);

            foreach (var context in contexts)
            {
                if (HasErrorResult(context))
                {
                    continue;
                }

                try
                {
                    if (context.Result is IFieldResult fieldResult)
                    {
                        context.Result = fieldResult.Value;
                    }

                    builder.Apply(context);
                }
                catch (Exception ex)
                {
                    context.ReportError(ex);
                    context.Result = null;
                }
            }
        };

    private static bool HasErrorResult(IMiddlewareContext context)
        => context.Result is IError or IEnumerable<IError> or IFieldResult { IsError: true }
            || (context.HasErrors && context.Result is null);

    internal static FieldMiddleware CreateDataMiddleware(IQueryBuilder builder)
        => next =>
        {
            return async ctx =>
            {
                builder.Prepare(ctx);

                // first lets invoke the rest of the pipeline to get the resolver result.
                await next(ctx).ConfigureAwait(false);

                // if the result is not a field result there is no need
                // to unwrap, and we can just invoke the data middleware.
                if (ctx.Result is not IFieldResult fieldResult)
                {
                    builder.Apply(ctx);
                }

                // if we however have a field result we will only invoke
                // the data middleware if it's a success result.
                else if (fieldResult.IsSuccess)
                {
                    // we need to unwrap the success result.
                    ctx.Result = fieldResult.Value;

                    // only after that we can invoke the data middleware.
                    builder.Apply(ctx);
                }
            };
        };
}
