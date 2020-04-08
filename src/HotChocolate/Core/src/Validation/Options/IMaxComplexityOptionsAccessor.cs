using System;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public delegate int ComplexityCalculation(
        IOutputField field,
        CostDirective? cost,
        int fieldDepth,
        int nodeDepth,
        Func<string, object?> getVariable);

    public interface IMaxComplexityOptionsAccessor
    {
        int? MaxAllowedComplexity { get; }

        ComplexityCalculation Calculation { get; }
    }

    public class MaxComplexityOptions : IMaxComplexityOptionsAccessor
    {
        public MaxComplexityOptions()
        {

        }

        public int? MaxAllowedComplexity { get; set; }

        public ComplexityCalculation Calculation { get; set; }
    }
}
