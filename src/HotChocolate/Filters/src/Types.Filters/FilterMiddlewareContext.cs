using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterMiddlewareContext
    {
        public FilterMiddlewareContext(string argumentName)
        {
            ArgumentName = argumentName;
        }

        public string ArgumentName { get; }

        public static FilterMiddlewareContext Create(string argumentName)
        {
            return new FilterMiddlewareContext(argumentName);
        }
    }
}
