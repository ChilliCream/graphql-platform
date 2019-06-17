using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotChocolate.Client.Core.Introspection
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DirectiveLocation
    {
        [EnumMember(Value = "QUERY")]
        Query,
        [EnumMember(Value = "MUTATION")]
        Mutation,
        [EnumMember(Value = "FIELD")]
        Field,
        [EnumMember(Value = "FRAGMENT_DEFINITION")]
        FragmentDefinition,
        [EnumMember(Value = "FRAGMENT_SPREAD")]
        FragmentSpread,
        [EnumMember(Value = "INLINE_FRAGMENT")]
        InlineFragment,
    }
}
