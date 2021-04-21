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
            => GetPagingArguments(context, PagingDefaults.DefaultPageSize);

        /// <summary>
        /// Gets the paging arguments.
        /// </summary>
        /// <param name="context">The current resolver context.</param>
        /// <param name="defaultPageSize">
        /// If neither 'first' or 'last' have been specified as an argument, 
        /// this will be used as 'first'.
        /// </param>
        /// <returns>Returns the paging arguments.</returns>
        public static CursorPagingArguments GetPagingArguments(this IResolverContext context, int defaultPageSize)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            var last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (first is null && last is null)
            {
                first = defaultPageSize;
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