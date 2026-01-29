using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

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
        ArgumentNullException.ThrowIfNull(context);

        var argumentName = context.Selection.SortingArgumentName;
        if (string.IsNullOrEmpty(argumentName))
        {
            return null;
        }

        var argument = context.Selection.Field.Arguments[argumentName];
        var sorting = context.ArgumentLiteral<IValueNode>(argumentName);

        if (argument.Type is not ListType listType
            || listType.ElementType().NamedType() is not ISortInputType sortingInput)
        {
            return null;
        }

        var sortingContext = new SortingContext(context, sortingInput, sorting, context.Service<InputParser>());

        // disable the execution of sorting by default
        sortingContext.Handled(true);

        return sortingContext;
    }
}
