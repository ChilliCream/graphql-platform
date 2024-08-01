namespace HotChocolate.Diagnostics;

[Flags]
public enum RequestDetails
{
    None = 0,
    Id = 1,
    Hash = 2,
    Operation = 4,
    Variables = 8,
    Extensions = 16,
    Query = 32,
    Default = Id | Hash | Operation | Extensions,
    All = Id | Hash | Operation | Variables | Extensions | Query,
}
