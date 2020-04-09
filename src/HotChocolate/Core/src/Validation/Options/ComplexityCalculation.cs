using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public delegate int ComplexityCalculation(
        IOutputField field,
        FieldNode selection,
        CostDirective? cost,
        int fieldDepth,
        int nodeDepth,
        Func<string, object?> getVariable,
        IMaxComplexityOptionsAccessor options);
}
