using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortMiddlewareContext
    {
        public SortMiddlewareContext(string argumentName)
        {
            ArgumentName = argumentName;
        }

        public string ArgumentName { get; }

        public static SortMiddlewareContext Create(string argumentName)
        {
            return new SortMiddlewareContext(argumentName);
        }
    }
}
