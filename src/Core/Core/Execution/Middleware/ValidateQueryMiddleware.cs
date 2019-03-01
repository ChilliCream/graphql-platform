using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Runtime;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class ValidateQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryValidator _validator;
        private readonly Cache<QueryValidationResult> _validatorCache;
        private readonly QueryExecutionDiagnostics _diagnostics;

        public ValidateQueryMiddleware(
            QueryDelegate next,
            IQueryValidator validator,
            Cache<QueryValidationResult> validatorCache,
            QueryExecutionDiagnostics diagnostics)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _validator = validator ??
                throw new ArgumentNullException(nameof(validator));
            _validatorCache = validatorCache ??
                new Cache<QueryValidationResult>(Defaults.CacheSize);
            _diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = _diagnostics.BeginValidation(context);

            if (context.Document == null)
            {
                context.Result = QueryResult.CreateError(new Error
                {
                    Message = CoreResources.ValidateQueryMiddleware_NoDocument
                });
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
                    _diagnostics.ValidationError(context);
                }
                else
                {
                    await _next(context).ConfigureAwait(false);
                }
            }

            _diagnostics.EndValidation(activity, context);
        }

        private QueryValidationResult Validate(
            ISchema schema,
            DocumentNode document)
        {
            return _validator.Validate(schema, document);
        }
    }
}
