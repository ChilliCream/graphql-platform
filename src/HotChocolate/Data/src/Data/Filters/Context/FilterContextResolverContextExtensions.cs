using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Filters;

public static class FilterContextResolverContextExtensions
{
    public static IFilterContext? GetFilterContext(this IResolverContext context)
    {
        IObjectField field = context.Selection.Field;
        if (!field.ContextData.TryGetValue(ContextArgumentNameKey, out var argumentNameObj) ||
            argumentNameObj is not NameString argumentName)
        {
            return null;
        }

        IInputField argument = context.Selection.Field.Arguments[argumentName];
        IValueNode filter = context.LocalContextData.ContainsKey(ContextValueNodeKey) &&
            context.LocalContextData[ContextValueNodeKey] is IValueNode node
                ? node
                : context.ArgumentLiteral<IValueNode>(argumentName);

        if (argument.Type is not IFilterInputType filterInput)
        {
            return null;
        }

        return new FilterContext(filter, filterInput, context.Service<InputParser>());
    }
}
