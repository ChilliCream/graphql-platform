using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Common extension of <see cref="IResolverContext" /> for sorting context
/// </summary>
public static class SortingContextResolverContextExtensions
{
    /// <summary>
    /// Creates a <see cref="SortingContext" /> from the sorting argument.
    /// </summary>
    public static ISortingContext? GetSortingContext(this IResolverContext context)
    {
        IObjectField field = context.Selection.Field;
        if (!field.ContextData.TryGetValue(ContextArgumentNameKey, out var argumentNameObj) ||
            argumentNameObj is not NameString argumentName)
        {
            return null;
        }

        IInputField argument = context.Selection.Field.Arguments[argumentName];
        IValueNode sorting = context.ArgumentLiteral<IValueNode>(argumentName);

        if (argument.Type is not ListType listType ||
            listType.ElementType().NamedType() is not ISortInputType sortingInput)
        {
            return null;
        }

        SortingContext sortingContext =
            new(context, sortingInput, sorting, context.Service<InputParser>());

        // disable the execution of sorting by default
        sortingContext.Handled(true);

        return sortingContext;
    }
}
