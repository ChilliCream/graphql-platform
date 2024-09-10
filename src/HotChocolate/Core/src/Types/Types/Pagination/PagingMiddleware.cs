using System.Collections.Immutable;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Pagination;

public class PagingMiddleware(FieldDelegate next, IPagingHandler pagingHandler)
{
    private readonly FieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly IPagingHandler _pagingHandler = pagingHandler ??
        throw new ArgumentNullException(nameof(pagingHandler));

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        _pagingHandler.ValidateContext(context);
        _pagingHandler.PublishPagingArguments(context);

        await _next(context).ConfigureAwait(false);

        // if the result is a field result, and we are going to unwrap it first.
        if (context.Result is IFieldResult fieldResult)
        {
            if (fieldResult.IsError)
            {
                return;
            }

            context.Result = fieldResult.Value;
        }

        // if the result was not short-circuited by another middleware or the resolver itself
        // we will try to apply paging.
        if (context.Result is not null and not IPage)
        {
            try
            {
                context.Result = await _pagingHandler
                    .SliceAsync(context, context.Result)
                    .ConfigureAwait(false);
            }
            catch (GraphQLException ex)
            {
                var errors = new IError[ex.Errors.Count];

                for (var i = 0; i < ex.Errors.Count; i++)
                {
                    errors[i] = ErrorBuilder
                        .FromError(ex.Errors[i])
                        .SetLocations(context.Selection.SyntaxNodes)
                        .SetPath(context.Path)
                        .Build();
                }

                throw new GraphQLException(errors);
            }
        }

        // if there are paging observers registered we will notify them.
        var observers = context.GetLocalStateOrDefault(
            WellKnownContextData.PagingObserver,
            ImmutableArray<IPageObserver>.Empty);

        if (observers.Length > 0 && context.Result is IPage page)
        {
            foreach (var observer in observers)
            {
                page.Accept(observer);
            }
        }
    }
}
