using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Authorization.Opa;

namespace HotChocolate.AspNetCore.Authorization;

public abstract class ResponseBase { }

public sealed class QueryResponse : ResponseBase
{
    public Guid? DecisionId { get; set; }
    [JsonConverter(typeof(OpaResultFieldConverter))]
    public bool? Result { get; set; }
}

public sealed class PolicyNotFound : ResponseBase
{
    private PolicyNotFound() {}
    public static readonly PolicyNotFound Response = new();
}


public sealed class NoDefaultPolicy : ResponseBase
{
    private NoDefaultPolicy() { }
    public static readonly NoDefaultPolicy Response = new();
}
