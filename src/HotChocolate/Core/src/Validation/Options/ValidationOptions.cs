using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public class ValidationOptions : IMaxExecutionDepthOptionsAccessor
    {
        public ValidationOptions()
        {
            ComplexityCalculation = DefaultCalculation;
        }

        public IList<IDocumentValidatorRule> Rules { get; } =
            new List<IDocumentValidatorRule>();

        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        public int? MaxAllowedExecutionDepth { get; set; }

        public ComplexityCalculation ComplexityCalculation { get; set; }

        public static int DefaultCalculation(ComplexityContext context)
        {
            if (context.Multipliers.Count == 0)
            {
                return context.Complexity + context.ChildComplexity;
            }

            var cost = context.Complexity;
            var childCost = context.ChildComplexity;

            foreach (MultiplierPathString multiplier in context.Multipliers)
            {
                if (context.TryGetArgumentValue(multiplier, out int value))
                {
                    cost *= value;
                    childCost *= value;
                }
            }

            return cost + childCost;
        }
    }
}
