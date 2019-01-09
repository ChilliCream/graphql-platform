using System;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

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
                ?? new ComplexityCalculation(
                    (fieldDef, field, path, cost) => cost.Complexity);
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

            return QueryValidationResult.OK;
        }
    }

    public delegate int ComplexityCalculation(
        IOutputField fieldDefinition,
        FieldNode fieldSelection,
        ICollection<IOutputField> path,
        CostDirective cost);
}
