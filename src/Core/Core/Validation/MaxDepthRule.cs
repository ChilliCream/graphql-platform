using System;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class MaxDepthRule
        : IQueryValidationRule
    {
        private readonly IValidateQueryOptionsAccessor _options;

        public MaxDepthRule(IValidateQueryOptionsAccessor options)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
        }

        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            if (_options.MaxExecutionDepth.HasValue)
            {
                var visitor = new MaxDepthVisitor(_options);
                visitor.Visit(queryDocument);

                return visitor.IsMaxDepthReached
                    ? new QueryValidationResult(
                        new ValidationError(
                            "The query exceded the maximum allowed execution " +
                            $"depth of {_options.MaxExecutionDepth.Value}.",
                            visitor.ViolatingFields.ToArray()))
                    : QueryValidationResult.OK;
            }
            return QueryValidationResult.OK;
        }
    }
}
