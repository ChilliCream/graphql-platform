using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;
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
            if (context.Document == null)
            {
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.ValidateQueryMiddleware_NoDocument)
                        .SetCode(MiddlewareErrorCodes.Incomplete)
                        .Build());
            }
            else
            {
                Activity activity = _diagnostics.BeginValidation(context);
                try
                {
                    context.ValidationResult = _validatorCache.GetOrCreate(
                        context.QueryKey,
                        () => Validate(context.Schema, context.Document));
                }
                finally
                {
                    _diagnostics.EndValidation(activity, context);
                }

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

        }

        private QueryValidationResult Validate(
            ISchema schema,
            DocumentNode document)
        {
            return _validator.Validate(schema, document);
        }
    }
}
