using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class ValidateQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IValidateQueryOptionsAccessor _options;
        private readonly IQueryValidator _validator;
        private readonly Cache<QueryValidationResult> _validatorCache;

        public ValidateQueryMiddleware(
            QueryDelegate next,
            IQueryValidator validator,
            Cache<QueryValidationResult> validatorCache,
            IValidateQueryOptionsAccessor options)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _validator = validator ??
                throw new ArgumentNullException(nameof(validator));
            _validatorCache = validatorCache ??
                new Cache<QueryValidationResult>(Defaults.CacheSize);
            _options = options ??
                throw new ArgumentNullException(nameof(options));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (context.Document == null)
            {
                context.Result = new QueryResult(new QueryError(
                    "The validation middleware expectes the " +
                    "query document to be parsed."));
                return Task.CompletedTask;
            }

            context.ValidationResult = _validatorCache.GetOrCreate(
                context.Request.Query,
                () => _validator.Validate(context.Schema, context.Document));

            if (context.ValidationResult.HasErrors)
            {
                context.Result = QueryResult.CreateError(
                    context.ValidationResult.Errors);
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
