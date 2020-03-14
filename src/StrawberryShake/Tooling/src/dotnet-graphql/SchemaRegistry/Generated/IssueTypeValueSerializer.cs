using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class IssueTypeValueSerializer
        : IValueSerializer
    {
        public string Name => "IssueType";

        public ValueKind Kind => ValueKind.Enum;

        public Type ClrType => typeof(IssueType);

        public Type SerializationType => typeof(string);

        public object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            var enumValue = (IssueType)value;

            switch(enumValue)
            {
                case IssueType.Information:
                    return "INFORMATION";
                case IssueType.Warning:
                    return "WARNING";
                case IssueType.Error:
                    return "ERROR";
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
                case "INFORMATION":
                    return IssueType.Information;
                case "WARNING":
                    return IssueType.Warning;
                case "ERROR":
                    return IssueType.Error;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}
