using HotChocolate.Execution.Instrumentation;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly DocumentValidator _documentValidator;

    private DocumentValidationMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] DocumentValidator documentValidator)
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
        if (context.Document is null || OperationDocumentId.IsNullOrEmpty(context.DocumentId))
        {
            context.Result = StateInvalidForDocumentValidation();
        }
        else
        {
            if (context.ValidationResult is null || _documentValidator.HasNonCacheableRules)
            {
                using (_diagnosticEvents.ValidateDocument(context))
                {
                    context.ValidationResult =
                        _documentValidator.Validate(
                            context.Schema,
                            context.DocumentId.Value,
                            context.Document,
                            context.Features);

                    if (!context.IsValidDocument)
                    {
                        // if the validation failed we will report errors within the validation
                        // span and we will complete the pipeline since we do not have a valid
                        // GraphQL request.
                        var validationResult = context.ValidationResult;

                        // create result context data that indicate that validation has failed.
                        var resultContextData = new Dictionary<string, object?>
                        {
                            { ValidationErrors, true },
                        };

                        // if one of the validation rules proposed a status code we will add
                        // it as a proposed status code to the result context data.
                        // depending on the transport this code might not be relevant or
                        // is even overruled.
                        if (context.ContextData.TryGetValue(HttpStatusCode, out var value))
                        {
                            resultContextData.Add(HttpStatusCode, value);
                        }

                        context.Result = OperationResultBuilder.CreateError(
                            validationResult.Errors,
                            resultContextData);

                        _diagnosticEvents.ValidationErrors(context, validationResult.Errors);
                        return;
                    }
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var documentValidator = core.SchemaServices.GetRequiredService<DocumentValidator>();
            var middleware = Create(next, diagnosticEvents, documentValidator);
            return context => middleware.InvokeAsync(context);
        };

    internal static DocumentValidationMiddleware Create(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        DocumentValidator documentValidator)
        => new(next, diagnosticEvents, documentValidator);
}
