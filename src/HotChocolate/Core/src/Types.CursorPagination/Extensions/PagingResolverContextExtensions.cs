using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public static class PagingResolverContextExtensions
    {
        /// <summary>
        /// Gets the paging arguments.
        /// </summary>
        /// <param name="context">The current resolver context.</param>
        /// <returns>Returns the paging arguments.</returns>
        public static CursorPagingArguments GetPagingArguments(this IResolverContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            var last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (first is null && last is null)
            {
                first = PagingDefaults.DefaultPageSize;
            }

            var arguments = new CursorPagingArguments(
                first,
                last,
                context.ArgumentValue<string?>(CursorPagingArgumentNames.After),
                context.ArgumentValue<string?>(CursorPagingArgumentNames.Before));

            return arguments;
        }
    }
}