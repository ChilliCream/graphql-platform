using System;

namespace HotChocolate.Execution.Pipeline.Complexity;

internal delegate int ComplexityAnalyzerDelegate(
    IServiceProvider services,
    IVariableValueCollection variableValues);
