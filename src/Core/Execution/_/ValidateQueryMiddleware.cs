using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class ValidateQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryValidator _validator;
        private readonly Cache<QueryValidationResult> _validatorCache;

        public ValidateQueryMiddleware(
            QueryDelegate next,
            IQueryValidator validator,
            Cache<QueryValidationResult> validatorCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _validator = validator
                ?? throw new ArgumentNullException(nameof(validator));
            _validatorCache = validatorCache
                ?? throw new ArgumentNullException(nameof(validatorCache));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (context.Document == null)
            {
                // TODO : Resources
                throw new QueryException(
                    "The validation pipeline expectes the " +
                    "query document to be parsed.");
            }

            context.ValidationResult = _validatorCache.GetOrCreate(
                context.Request.Query, () => Validate(context.Document));

            if (context.ValidationResult.HasErrors)
            {
                return Task.CompletedTask;
            }
            return _next(context);
        }

        private QueryValidationResult Validate(DocumentNode document)
        {
            return _validator.Validate(document);
        }
    }
}

