namespace Prometheus.Validation
{
    public class SuccessResult
        : IValidationResult
    {
        public SuccessResult(IValidationRule rule)
        {
            if (rule == null)
            {
                throw new System.ArgumentNullException(nameof(rule));
            }

            Rule = rule;
        }

        public IValidationRule Rule { get; }
    }
}