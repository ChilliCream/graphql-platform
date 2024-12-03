using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Common extension of <see cref="IResolverContext" /> for filter context
/// </summary>
public static class FilterContextResolverContextExtensions
{
    /// <summary>
    /// Creates a <see cref="FilterContext" /> from the filter argument.
    /// </summary>
    public static IFilterContext? GetFilterContext(this IResolverContext context)
    {
        var field = context.Selection.Field;
        if (!field.ContextData.TryGetValue(ContextArgumentNameKey, out var argumentNameObj) ||
            argumentNameObj is not string argumentName)
        {
            return null;
        }

        var argument = context.Selection.Field.Arguments[argumentName];
        var filter = context.LocalContextData.ContainsKey(ContextValueNodeKey) &&
            context.LocalContextData[ContextValueNodeKey] is IValueNode node
                ? node
                : context.ArgumentLiteral<IValueNode>(argumentName);

        if (argument.Type is not IFilterInputType filterInput)
        {
            return null;
        }

        var filterContext = new FilterContext(context, filterInput, filter, context.Service<InputParser>());

        // disable the execution of filtering by default
        filterContext.Handled(true);

        return filterContext;
    }
}
