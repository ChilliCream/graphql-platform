using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a filter definition
/// </summary>
public class ElasticSearchFilterProvider
    : FilterProvider<ElasticSearchFilterVisitorContext>
{
    /// <inheritdoc />
    public ElasticSearchFilterProvider()
    {
    }

    /// <inheritdoc />
    public ElasticSearchFilterProvider(
        Action<IFilterProviderDescriptor<ElasticSearchFilterVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to traverse the incoming selection set an execute handlers
    /// </summary>
    protected virtual FilterVisitor<ElasticSearchFilterVisitorContext, ISearchOperation>
        Visitor
    { get; } = new(new ElasticSearchFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            ElasticSearchFilterVisitorContext? visitorContext = null;
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                visitorContext = new ElasticSearchFilterVisitorContext(filterInput);

                Visitor.Visit(filter, visitorContext);

                if (visitorContext.Errors.Count > 0)
                {
                    context.Result = Array.Empty<TEntityType>();
                    foreach (var error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }
                else
                {
                    if (!visitorContext.TryCreateQuery(out BoolOperation? whereQuery) ||
                        visitorContext.Errors.Count > 0)
                    {
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                    await next(context).ConfigureAwait(false);


                    if (context.Result is IElasticSearchExecutable executable)
                    {
                        context.Result = executable.WithFiltering(whereQuery!);
                    }
                }
            }
            else
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}
