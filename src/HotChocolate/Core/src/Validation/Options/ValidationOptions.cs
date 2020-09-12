using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public class ValidationOptions
        : IMaxComplexityOptionsAccessor
        , IMaxExecutionDepthOptionsAccessor
    {
        public ValidationOptions()
        {
            ComplexityCalculation = DefaultCalculation;
        }

        public IList<IDocumentValidatorRule> Rules { get; } =
            new List<IDocumentValidatorRule>();

        public int DefaultComplexity { get; set; } = 1;

        public int? MaxAllowedComplexity { get; set; }

        public bool UseComplexityMultipliers { get; set; }

        public ComplexityCalculation ComplexityCalculation { get; set; }

        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        public int? MaxAllowedExecutionDepth { get; set; }

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
                return options.DefaultComplexity;
            }

            if (options.UseComplexityMultipliers)
            {
                if (cost.Multipliers.Count == 0)
                {
                    return cost.Complexity;
                }

                var complexity = 0;

                for (var i = 0; i < cost.Multipliers.Count; i++)
                {
                    MultiplierPathString multiplier = cost.Multipliers[i];
                    ArgumentNode argument = selection.Arguments.FirstOrDefault(t =>
                        t.Name.Value.Equals(multiplier.Value));

                    if (argument is { } && argument.Value is { })
                    {
                        switch (argument.Value)
                        {
                            case VariableNode variable:
                                complexity += getVariable(variable.Value) switch
                                {
                                    int m => m * cost.Complexity,
                                    double m => (int)(m * cost.Complexity),
                                    _ => cost.Complexity
                                };
                                break;

                            case IntValueNode intValue:
                                complexity += intValue.ToInt32() * cost.Complexity;
                                break;

                            case FloatValueNode floatValue:
                                complexity += (int)(floatValue.ToDouble() * cost.Complexity);
                                break;

                            default:
                                complexity += cost.Complexity;
                                break;
                        }
                    }
                    else
                    {
                        complexity += cost.Complexity;
                    }
                }

                return complexity;
            }

            return cost.Complexity;
        }
    }
}
