using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public delegate int ComplexityCalculation(
        IOutputField field,
        CostDirective? cost,
        int fieldDepth,
        int nodeDepth,
        Func<string, object?> getVariable,
        IMaxComplexityOptionsAccessor options);

    public interface IMaxComplexityOptionsAccessor
    {
        int DefaultCost { get; }

        int? MaxAllowedComplexity { get; }

        bool UseComplexityMultipliers { get; }

        ComplexityCalculation Calculation { get; }
    }

    public class MaxComplexityOptions : IMaxComplexityOptionsAccessor
    {
        public MaxComplexityOptions()
        {
            ComplexityCalculation
        }

        public int DefaultCost { get; set; }

        public int? MaxAllowedComplexity { get; set; }

        public bool UseComplexityMultipliers { get; set; }

        public ComplexityCalculation Calculation { get; set; }

        public static int DefaultCalculation(
            IOutputField field,
            FieldNode selection,
            CostDirective? cost,
            int fieldDepth,
            int nodeDepth,
            Func<string, object?> getVariable,
            IMaxComplexityOptionsAccessor options)
        {
            if (cost is null)
            {
                return options.DefaultCost;
            }

            if (options.UseComplexityMultipliers)
            {
                if (cost.Multipliers.Count == 0)
                {
                    return cost.Complexity;
                }

                int complexity = 0;

                for (int i = 0; i < cost.Multipliers.Count; i++)
                {
                    MultiplierPathString multiplier =  cost.Multipliers[i];
                    ArgumentNode argument = selection.Arguments.FirstOrDefault(t => t.Name.Value.Equals(multiplier.Value));
                    if (MultiplierHelper.TryGetMultiplierValue(
                        context.FieldSelection, context.Variables,
                        context.Cost.Multipliers[i], out int multiplier))
                    {
                        complexity += context.Cost.Complexity * multiplier;
                    }
                    else
                    {
                        complexity += context.Cost.Complexity;
                    }
                }

                return complexity;
            }
        }
    }

    public interface IMaxExecutionDepthOptionsAccessor
    {
        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        int? MaxAllowedExecutionDepth { get; }
    }
}
