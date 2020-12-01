using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class HashFormatValueSerializer
        : IValueSerializer
    {
        public string Name => "HashFormat";

        public ValueKind Kind => ValueKind.Enum;

        public Type ClrType => typeof(HashFormat);

        public Type SerializationType => typeof(string);

        public object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            var enumValue = (HashFormat)value;

            switch(enumValue)
            {
                case HashFormat.Base64:
                    return "BASE64";
                case HashFormat.Hex:
                    return "HEX";
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
                case "BASE64":
                    return HashFormat.Base64;
                case "HEX":
                    return HashFormat.Hex;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}
