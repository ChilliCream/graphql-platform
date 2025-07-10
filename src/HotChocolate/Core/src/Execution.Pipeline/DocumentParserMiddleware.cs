using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentParserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDocumentHashProvider _documentHashProvider;
    private readonly IErrorHandler _errorHandler;
    private readonly ParserOptions _parserOptions;

    private DocumentParserMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IDocumentHashProvider documentHashProvider,
        IErrorHandler errorHandler,
        ParserOptions parserOptions)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(documentHashProvider);
        ArgumentNullException.ThrowIfNull(errorHandler);
        ArgumentNullException.ThrowIfNull(parserOptions);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _documentHashProvider = documentHashProvider;
        _errorHandler = errorHandler;
        _parserOptions = parserOptions;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is null && context.Request.Document is not null)
        {
            var success = false;
            var query = context.Request.Document;

            // a parsed document was passed into the request.
            if (query is OperationDocument parsed)
            {
                documentInfo.Hash = CreateDocumentHash(documentInfo, context.Request);
                documentInfo.Document = parsed.Document;
                success = true;
            }
            else if (query is OperationDocumentSourceText source)
            {
                using (_diagnosticEvents.ParseDocument(context))
                {
                    try
                    {
                        documentInfo.Hash = CreateDocumentHash(documentInfo, context.Request);
                        documentInfo.Document = Utf8GraphQLParser.Parse(source.SourceText, _parserOptions);
                        success = true;
                    }
                    catch (SyntaxException ex)
                    {
                        // if we have syntax errors, we will report them within the
                        // parse document diagnostic span.
                        var error = _errorHandler.Handle(
                            ErrorBuilder.New()
                                .SetMessage(ex.Message)
                                .SetCode(ErrorCodes.Execution.SyntaxError)
                                .AddLocation(new Location(ex.Line, ex.Column))
                                .Build());

                        context.Result = OperationResultBuilder.CreateError(error);
                        _diagnosticEvents.ExecutionError(context, ErrorKind.SyntaxError, [error]);
                    }
                }
            }
            else
            {
                throw ErrorHelper.QueryTypeNotSupported();
            }

            if (success)
            {
                if (documentInfo.Id.IsEmpty)
                {
                    documentInfo.Id = new OperationDocumentId(documentInfo.Hash.Value);
                }

                await _next(context).ConfigureAwait(false);
            }
        }
        else
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private OperationDocumentHash CreateDocumentHash(
        OperationDocumentInfo documentInfo,
        IOperationRequest request)
    {
        if (!documentInfo.Hash.IsEmpty)
        {
            return documentInfo.Hash;
        }

        if (!request.DocumentHash.IsEmpty)
        {
            return request.DocumentHash;
        }

        return _documentHashProvider.ComputeHash(request.Document!.AsSpan());
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var documentHashProvider = core.SchemaServices.GetRequiredService<IDocumentHashProvider>();
                var errorHandler = core.SchemaServices.GetRequiredService<IErrorHandler>();
                var parserOptions = core.SchemaServices.GetRequiredService<ParserOptions>();
                var middleware = Create(next, diagnosticEvents, documentHashProvider, errorHandler, parserOptions);
                return context => middleware.InvokeAsync(context);
            },
            nameof(DocumentParserMiddleware));

    internal static DocumentParserMiddleware Create(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IDocumentHashProvider documentHashProvider,
        IErrorHandler errorHandler,
        ParserOptions parserOptions)
        => new(next, diagnosticEvents, documentHashProvider, errorHandler, parserOptions);
}
