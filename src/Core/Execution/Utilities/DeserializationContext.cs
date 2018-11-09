using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Execution
{
    internal class DeserializationContext
    {
        public object Object { get; set; }

        public Type Type { get; set; }

        public ILookup<string, PropertyInfo> Fields { get; set; }
    }
}
