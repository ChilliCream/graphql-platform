using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Sorting
{
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
