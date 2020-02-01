namespace HotChocolate.Validation
{
    public static class Complexity
    {
        public static int DefaultCalculation(ComplexityContext context)
        {
            return context.Cost.Complexity;
        }

        public static int MultiplierCalculation(ComplexityContext context)
        {
            if (context.Cost.Multipliers.Count == 0)
            {
                return context.Cost.Complexity;
            }

            int complexity = 0;

            for (int i = 0; i < context.Cost.Multipliers.Count; i++)
            {
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
