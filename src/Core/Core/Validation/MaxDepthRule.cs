using System;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class MaxDepthRule
        : IQueryValidationRule
    {
        private readonly IValidateQueryOptionsAccessor _options;
        private readonly MaxDepthVisitor _visitor;

        public MaxDepthRule(IValidateQueryOptionsAccessor options)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _visitor = new MaxDepthVisitor(_options);
        }

        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            if (IsRuleEnabled())
            {
                IReadOnlyCollection<FieldNode> violatingFields =
                    _visitor.Visit(queryDocument);

                return violatingFields.Count == 0
                    ? QueryValidationResult.OK
                    : new QueryValidationResult(
                        new ValidationError(
                            "The query exceded the maximum allowed execution " +
                            $"depth of {_options.MaxExecutionDepth.Value}.",
                            violatingFields));
            }

            return QueryValidationResult.OK;
        }

        private bool IsRuleEnabled() => _options.MaxExecutionDepth.HasValue;
    }
}
