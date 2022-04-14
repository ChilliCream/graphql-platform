using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a
/// <see cref="FilterDefinition{T}"/>
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
        Visitor { get; } = new(new ElasticSearchFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            ElasticSearchFilterVisitorContext? visitorContext = null;
            IInputField argument = context.Selection.Field.Arguments[argumentName];
            IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                visitorContext = new ElasticSearchFilterVisitorContext(filterInput);

                Visitor.Visit(filter, visitorContext);

                if (!visitorContext.TryCreateQuery(out QueryDefinition? whereQuery) ||
                    visitorContext.Errors.Count > 0)
                {
                    context.Result = Array.Empty<TEntityType>();
                    foreach (IError error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }
                else
                {
                    // TODO wellknow context data
                    context.LocalContextData =
                        context.LocalContextData.SetItem(nameof(QueryDefinition), whereQuery);

                    await next(context).ConfigureAwait(false);
                }
            }
            else
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}

