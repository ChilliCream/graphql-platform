using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Utilities
{
    internal class ConverterContext
    {
        public object Object { get; set; }

        public Type Type { get; set; }

        public ILookup<string, PropertyInfo> Fields { get; set; }
    }
}
