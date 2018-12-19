using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public static class ResolverContextExtensions
    {
        public static IDictionary<string, object> GetCursorProperties(
            this IResolverContext context)
        {
            string cursor = context.Argument<string>("after")
                ?? context.Argument<string>("before");

            if (cursor == null)
            {
                return new Dictionary<string, object>();
            }

            return Base64Serializer
                .Deserialize<Dictionary<string, object>>(cursor);
        }
    }
}
