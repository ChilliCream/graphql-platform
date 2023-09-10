using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class QueryableSortMiddleware<T>
{
    private readonly SortMiddlewareContext _contextData;
    private readonly FieldDelegate _next;

    public QueryableSortMiddleware(
        FieldDelegate next,
        SortMiddlewareContext contextData)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _contextData = contextData ?? throw new ArgumentNullException(nameof(contextData));
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);

        var sortArg = context.ArgumentLiteral<IValueNode>(_contextData.ArgumentName);

        if (sortArg is NullValueNode)
        {
            return;
        }

        IQueryable<T>? source = null;

        if (context.Result is IQueryable<T> q)
        {
            source = q;
        }
        else if (context.Result is IEnumerable<T> e)
        {
            source = e.AsQueryable();
        }

        if (source is not null &&
            context.Selection.Field.Arguments[_contextData.ArgumentName].Type
                is InputObjectType iot
                and ISortInputType { EntityType: not null } fit)
        {
            var visitorCtx = new QueryableSortVisitorContext(
                context.Service<InputParser>(),
                iot,
                fit.EntityType,
                source is EnumerableQuery);

            QueryableSortVisitor.Default.Visit(sortArg, visitorCtx);

            source = visitorCtx.Sort(source);
            context.Result = source;
        }
    }
}