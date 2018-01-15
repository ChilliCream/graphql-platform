using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zeus.Types
{
    internal static class BuiltInTypes
    {
        internal static readonly string String = "String";
        internal static readonly string Integer = "Int";

        private static readonly HashSet<string> _builtInTypes = new HashSet<string>
        {
            String,
            Integer
        };

        public static bool Contains(string name)
        {
            return _builtInTypes.Contains(name);
        }
    }
}