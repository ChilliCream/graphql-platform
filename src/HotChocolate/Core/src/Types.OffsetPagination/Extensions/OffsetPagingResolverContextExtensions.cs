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
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
            var take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);

            var arguments = new OffsetPagingArguments(skip, take ?? PagingDefaults.DefaultPageSize);

            return arguments;
        }
    }
}