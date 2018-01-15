using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zeus.Types
{
    internal static class ScalarTypes
    {
        internal static readonly string Integer = "Int";
        internal static readonly string Float = "Float";
        internal static readonly string String = "String";
        internal static readonly string Boolean = "Boolean";
        internal static readonly string ID = "ID";

        private static readonly HashSet<string> _builtInTypes = new HashSet<string>
        {
            Integer,
            Float,
            String,
            Boolean,
            ID
        };

        public static bool Contains(string name)
        {
            return _builtInTypes.Contains(name);
        }
    }
}