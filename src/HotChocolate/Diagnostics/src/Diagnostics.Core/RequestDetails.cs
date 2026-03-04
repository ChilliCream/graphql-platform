namespace HotChocolate.Diagnostics;

[Flags]
public enum RequestDetails
{
    None = 0,
    Id = 1,
    Hash = 2,
    OperationName = 4,
    Variables = 8,
    Extensions = 16,
    Query = 32,
    Default = Id | Hash | OperationName | Extensions,
    All = Id | Hash | OperationName | Variables | Extensions | Query
}
