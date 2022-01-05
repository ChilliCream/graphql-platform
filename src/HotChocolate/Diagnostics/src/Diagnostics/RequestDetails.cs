using System;

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

[Flags]
public enum ActivityScopes
{
    None = 0,
    ParseRequest = 1,
    ParseDocument = 2,
    ValidateDocument = 4,
    AnalyzeComplexity = 8,
    CoerceVariables = 16,
    CompileOperation = 32,
    ExecuteOperation = 64,
    ResolveFieldValue = 128,
    All = ParseRequest |
          ParseDocument |
          ValidateDocument |
          AnalyzeComplexity |
          CoerceVariables |
          CompileOperation |
          ExecuteOperation |
          ResolveFieldValue
}
