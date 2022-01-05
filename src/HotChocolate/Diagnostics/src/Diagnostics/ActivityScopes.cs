using System;

namespace HotChocolate.Diagnostics;

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
