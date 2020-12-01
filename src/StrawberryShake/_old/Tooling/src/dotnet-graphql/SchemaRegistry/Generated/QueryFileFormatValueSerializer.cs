using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class QueryFileFormatValueSerializer
        : IValueSerializer
    {
        public string Name => "QueryFileFormat";

        public ValueKind Kind => ValueKind.Enum;

        public Type ClrType => typeof(QueryFileFormat);

        public Type SerializationType => typeof(string);

        public object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            var enumValue = (QueryFileFormat)value;

            switch(enumValue)
            {
                case QueryFileFormat.Graphql:
                    return "GRAPHQL";
                case QueryFileFormat.Relay:
                    return "RELAY";
                default:
                    throw new NotSupportedException();
            }
        }

        public object? Deserialize(object? serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            var stringValue = (string)serialized;

            switch(stringValue)
            {
                case "GRAPHQL":
                    return QueryFileFormat.Graphql;
                case "RELAY":
                    return QueryFileFormat.Relay;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}
