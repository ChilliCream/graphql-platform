using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class MaxComplexityRule
        : IQueryValidationRule
    {
        private readonly IValidateQueryOptionsAccessor _options;
        private readonly ComplexityCalculation _calculateComplexity;
        private readonly MaxComplexityVisitor _visitor =
            new MaxComplexityVisitor();

        public MaxComplexityRule(
            IValidateQueryOptionsAccessor options,
            ComplexityCalculation calculateComplexity)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _calculateComplexity = calculateComplexity
                ?? Complexity.MultiplierCalculation;
        }

        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            if (IsRuleEnabled())
            {
                int complexity = _visitor.Visit(
                    queryDocument,
                    schema,
                    _calculateComplexity);

                if (complexity > _options.MaxOperationComplexity)
                {
                    return new QueryValidationResult(new ValidationError(
                        "At least one operation of the query document had a " +
                        $"complexity of {complexity}. \n" +
                        "The maximum allowed query complexity is " +
                        $"{_options.MaxOperationComplexity}."));
                }
            }

            return QueryValidationResult.OK;
        }

        private bool IsRuleEnabled()
        {
            if (_options.UseComplexityMultipliers == true)
            {
                return false;
            }

            return _options.MaxOperationComplexity.HasValue;
        }
    }
}
