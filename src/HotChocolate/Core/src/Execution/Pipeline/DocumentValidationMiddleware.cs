using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class DocumentValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IDocumentValidator _documentValidator;

        public DocumentValidationMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IDocumentValidator documentValidator)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _documentValidator = documentValidator ??
                throw new ArgumentNullException(nameof(documentValidator));
        }

        public async Task InvokeAsync(IRequestContext context)
        {
            if (context.Document is null && context.ValidationResult is null)
            {
                // TODO : ErrorHelper
                context.Result = QueryResultBuilder.CreateError(
                    ErrorBuilder.New()
                        .SetMessage("Cannot Validate")
                        .Build());
            }
            else
            {
                if (context.ValidationResult is null)
                {
                    using (_diagnosticEvents.ValidateDocument(context))
                    {
                        context.ValidationResult =
                            _documentValidator.Validate(context.Schema, context.Document!);
                    }
                }

                if (context.ValidationResult is { HasErrors: true })
                {
                    context.Result = QueryResultBuilder.CreateError(context.ValidationResult.Errors);
                    _diagnosticEvents.ValidationErrors(context, context.ValidationResult.Errors);
                }
                else
                {
                    await _next(context).ConfigureAwait(false);
                }
            }
        }
    }
}
