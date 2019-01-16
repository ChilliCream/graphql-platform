using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
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
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _validator = validator ??
                throw new ArgumentNullException(nameof(validator));
            _validatorCache = validatorCache ??
                new Cache<QueryValidationResult>(Defaults.CacheSize);
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = ValidationDiagnosticEvents
                .BeginValidation(context);

            if (context.Document == null)
            {
                // TODO : Resources
                context.Result = QueryResult.CreateError(new QueryError(
                    "The validation middleware expects the " +
                    "query document to be parsed."));
            }
            else
            {
                context.ValidationResult = _validatorCache.GetOrCreate(
                    context.Request.Query,
                    () => Validate(context.Schema, context.Document));

                if (context.ValidationResult.HasErrors)
                {
                    context.Result = QueryResult.CreateError(
                        context.ValidationResult.Errors);
                    ValidationDiagnosticEvents.ValidationError(context);
                }
                else
                {
                    await _next(context).ConfigureAwait(false);
                }
            }

            ValidationDiagnosticEvents.EndValidation(activity, context);
        }

        private QueryValidationResult Validate(
            ISchema schema,
            DocumentNode document)
        {
            return _validator.Validate(schema, document);
        }
    }
}
