using HotChocolate.Execution.Instrumentation;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly DocumentValidator _documentValidator;

    private DocumentValidationMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        DocumentValidator documentValidator)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(documentValidator);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _documentValidator = documentValidator;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is null || documentInfo.Id.IsEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForDocumentValidation();
        }
        else
        {
            if (!documentInfo.IsValidated || _documentValidator.HasNonCacheableRules)
            {
                using (_diagnosticEvents.ValidateDocument(context))
                {
                    var result =
                        _documentValidator.Validate(
                            context.Schema,
                            documentInfo.Id,
                            documentInfo.Document,
                            context.Features,
                            documentInfo.IsValidated);

                    documentInfo.IsValidated = !result.HasErrors;

                    if (result.HasErrors)
                    {
                        // create result context data that indicate that validation has failed.
                        var resultContextData = new Dictionary<string, object?>
                        {
                            { ExecutionContextData.ValidationErrors, true }
                        };

                        // if one of the validation rules proposed a status code, we will add
                        // it as a proposed status code to the result context data.
                        // depending on the transport, this code might not be relevant or
                        // is even overruled.
                        if (context.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value))
                        {
                            resultContextData.Add(ExecutionContextData.HttpStatusCode, value);
                        }

                        context.Result = OperationResultBuilder.CreateError(result.Errors, resultContextData);
                        _diagnosticEvents.ExecutionError(context, ErrorKind.ValidationError, result.Errors);
                        return;
                    }
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var documentValidator = core.SchemaServices.GetRequiredService<DocumentValidator>();
                var middleware = Create(next, diagnosticEvents, documentValidator);
                return context => middleware.InvokeAsync(context);
            },
            nameof(DocumentValidationMiddleware));

    internal static DocumentValidationMiddleware Create(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        DocumentValidator documentValidator)
        => new(next, diagnosticEvents, documentValidator);
}
