using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    public static class OffsetPagingResolverContextExtensions
    {
        /// <summary>
        /// Gets the offset paging arguments.
        /// </summary>
        /// <param name="context">The current resolver context.</param>
        /// <returns>Returns the offset paging arguments.</returns>
        public static OffsetPagingArguments GetOffsetPagingArguments(this IResolverContext context)
            => GetOffsetPagingArguments(context, PagingDefaults.DefaultPageSize);

        /// <summary>
        /// Gets the offset paging arguments.
        /// </summary>
        /// <param name="context">The current resolver context.</param>
        /// <param name="defaultPageSize">
        /// If no 'take' argument has been specified, 
        /// this will be used as 'take'.
        /// </param>
        /// <returns>Returns the offset paging arguments.</returns>
        public static OffsetPagingArguments GetOffsetPagingArguments(this IResolverContext context, int defaultPageSize)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
            var take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);

            var arguments = new OffsetPagingArguments(skip, take ?? defaultPageSize);

            return arguments;
        }
    }
}