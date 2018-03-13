using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public static class ScalarTypes
    {
        public static readonly string Integer = "Int";
        public static readonly string Float = "Float";
        public static readonly string String = "String";
        public static readonly string Boolean = "Boolean";
        public static readonly string ID = "ID";

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