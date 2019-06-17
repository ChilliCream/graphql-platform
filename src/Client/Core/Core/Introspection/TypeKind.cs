using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotChocolate.Client.Core.Introspection
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeKind
    {
        [EnumMember(Value = "SCALAR")]
        Scalar,
        [EnumMember(Value = "OBJECT")]
        Object,
        [EnumMember(Value = "UNION")]
        Union,
        [EnumMember(Value = "INTERFACE")]
        Interface,
        [EnumMember(Value = "ENUM")]
        Enum,
        [EnumMember(Value = "INPUT_OBJECT")]
        InputObject,
        [EnumMember(Value = "LIST")]
        List,
        [EnumMember(Value = "NON_NULL")]
        NonNull
    }
}
