using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Types;

namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// The complexity settings.
    /// </summary>
    public class ComplexityAnalyzerSettings : ICostSettings
    {
        /// <summary>
        /// Defines if the complexity analysis is enabled.
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum allowed complexity.
        /// </summary>
        public int MaximumAllowed { get; set; } = 1000;

        /// <summary>
        /// Defines if default cost and multipliers shall be applied to the schema.
        /// </summary>
        public bool ApplyDefaults { get; set; } = true;

        /// <summary>
        /// Gets or sets the complexity that is applied to all fields
        /// that do not have a cost directive.
        /// </summary>
        public int DefaultComplexity { get; set; } = 1;

        /// <summary>
        /// Gets or sets the complexity that is applied to async and data
        /// resolvers if <see cref="ApplyDefaults"/> is <c>true</c>.
        /// </summary>
        public int DefaultResolverComplexity { get; set; } = 5;

        /// <summary>
        /// Gets or sets the context data key that that will be used to store
        /// the calculated complexity on the request.
        /// </summary>
        public string ContextDataKey { get; set; } = WellKnownContextData.OperationComplexity;

        /// <summary>
        /// Gets or sets the complexity calculation delegate..
        /// </summary>
        public ComplexityCalculation Calculation { get; set; } = DefaultCalculation;

        /// <summary>
        /// The default complexity calculation algorithm.
        /// </summary>
        /// <param name="context">
        /// The complexity context.
        /// </param>
        /// <returns>
        /// Returns the calculated field complexity.
        /// </returns>
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
