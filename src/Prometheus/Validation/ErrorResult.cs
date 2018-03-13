using System;

namespace Prometheus.Validation
{
    public class ErrorResult
        : IValidationResult
    {
        public ErrorResult(IValidationRule rule, string message)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The warning message mustn't be null or string.Empty.",
                    nameof(message));
            }

            Rule = rule;
            Message = message;
        }

        public IValidationRule Rule { get; }
        public string Message { get; }
    }
}