using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using static HotChocolate.Execution.QueryResultBuilder;

namespace HotChocolate.Execution
{
    internal sealed class ValidateQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IDocumentValidator _validator;
        private readonly Cache<DocumentValidatorResult> _validatorCache;
        private readonly QueryExecutionDiagnostics _diagnostics;

        public ValidateQueryMiddleware(
            QueryDelegate next,
            IDocumentValidatorFactory validatorFactory,
            Cache<DocumentValidatorResult> validatorCache,
            QueryExecutionDiagnostics diagnostics)
        {
            if (validatorFactory == null)
            {
                throw new ArgumentNullException(nameof(validatorFactory));
            }

            _next = next ??  throw new ArgumentNullException(nameof(next));
            _validator = validatorFactory.CreateValidator();
            _validatorCache = validatorCache ??
                new Cache<DocumentValidatorResult>(Defaults.CacheSize);
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (context.Document == null)
            {
                context.Result = CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.ValidateQueryMiddleware_NoDocument)
                        .SetCode(ErrorCodes.Execution.Incomplete)
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
                    context.Result = QueryResultBuilder.CreateError(
                        context.ValidationResult.Errors);
                    _diagnostics.ValidationError(context);
                }
                else
                {
                    await _next(context).ConfigureAwait(false);
                }
            }
        }

        private DocumentValidatorResult Validate(ISchema schema, DocumentNode document)
        {
            return _validator.Validate(schema, document);
        }
    }
}
