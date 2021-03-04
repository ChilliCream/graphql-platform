using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class BuiltInScalarNames
    {
        public const string String = "String";
        public const string ID = "ID";
        public const string Boolean = "Boolean";
        public const string Byte = "Byte";
        public const string Short = "Short";
        public const string Int = "Int";
        public const string Long = "Long";
        public const string Float = "Float";
        public const string Decimal = "Decimal";
        public const string Url = "Url";
        public const string Uuid = "Uuid";
        public const string DateTime = "DateTime";
        public const string Date = "Date";
        public const string MultiplierPath = "MultiplierPath";
        public const string Name = "Name";
        public const string ByteArray = "ByteArray";
        public const string Any = "Any";
        public const string TimeSpan = "TimeSpan";

        private static readonly HashSet<string> _typeNames = new()
        {
            String,
            ID,
            Boolean,
            Byte,
            Short,
            Int,
            Long,
            Float,
            Decimal,
            Url,
            Uuid,
            DateTime,
            Date,
            MultiplierPath,
            Name,
            ByteArray,
            Any,
            TimeSpan
        };

        public static bool IsBuiltInScalar(string typeName) => _typeNames.Contains(typeName);
    }
}
