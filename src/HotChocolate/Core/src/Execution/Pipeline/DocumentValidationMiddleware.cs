using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Validation;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDocumentValidator _documentValidator;

    public DocumentValidationMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents,
        IDocumentValidator documentValidator)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _documentValidator = documentValidator ??
            throw new ArgumentNullException(nameof(documentValidator));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null)
        {
            context.Result = StateInvalidForDocumentValidation();
        }
        else
        {
            if (context.ValidationResult is null || _documentValidator.HasDynamicRules)
            {
                using (_diagnosticEvents.ValidateDocument(context))
                {
                    context.ValidationResult = _documentValidator.Validate(
                        context.Schema,
                        context.Document,
                        context.ContextData,
                        context.ValidationResult is not null);

                    if (!context.IsValidDocument)
                    {
                        // if the validation failed we will report errors within the validation
                        // span and we will complete the pipeline since we do not have a valid
                        // GraphQL request.
                        var validationResult = context.ValidationResult;

                        context.Result = QueryResultBuilder.CreateError(
                            validationResult.Errors,
                            new Dictionary<string, object?> { { ValidationErrors, true } });

                        _diagnosticEvents.ValidationErrors(context, validationResult.Errors);
                        return;
                    }
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
