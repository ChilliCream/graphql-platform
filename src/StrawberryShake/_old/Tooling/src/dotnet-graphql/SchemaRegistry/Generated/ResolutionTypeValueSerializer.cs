using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class ResolutionTypeValueSerializer
        : IValueSerializer
    {
        public string Name => "ResolutionType";

        public ValueKind Kind => ValueKind.Enum;

        public Type ClrType => typeof(ResolutionType);

        public Type SerializationType => typeof(string);

        public object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            var enumValue = (ResolutionType)value;

            switch(enumValue)
            {
                case ResolutionType.None:
                    return "NONE";
                case ResolutionType.Open:
                    return "OPEN";
                case ResolutionType.Fixed:
                    return "FIXED";
                case ResolutionType.Wontfixed:
                    return "WONTFIXED";
                case ResolutionType.Cannotbefixed:
                    return "CANNOTBEFIXED";
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
                case "NONE":
                    return ResolutionType.None;
                case "OPEN":
                    return ResolutionType.Open;
                case "FIXED":
                    return ResolutionType.Fixed;
                case "WONTFIXED":
                    return ResolutionType.Wontfixed;
                case "CANNOTBEFIXED":
                    return ResolutionType.Cannotbefixed;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}
