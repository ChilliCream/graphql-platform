using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentParserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDocumentHashProvider _documentHashProvider;
    private readonly ParserOptions _parserOptions;

    public DocumentParserMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents,
        IDocumentHashProvider documentHashProvider,
        ParserOptions parserOptions)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _documentHashProvider = documentHashProvider ??
            throw new ArgumentNullException(nameof(documentHashProvider));
        _parserOptions = parserOptions ??
            throw new ArgumentNullException(nameof(parserOptions));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null && context.Request.Query is not null)
        {
            var success = false;
            var query = context.Request.Query;

            // a parsed document was passed into the request.
            if (query is QueryDocument parsed)
            {
                context.DocumentId = ComputeDocumentHash(
                    context.DocumentHash,
                    context.Request.QueryHash,
                    context.Request.Query);
                context.Document = parsed.Document;
                success = true;
            }
            else if (query is QuerySourceText source)
            {
                using (_diagnosticEvents.ParseDocument(context))
                {
                    try
                    {
                        context.DocumentId = ComputeDocumentHash(
                            context.DocumentHash,
                            context.Request.QueryHash,
                            context.Request.Query);
                        context.Document = Utf8GraphQLParser.Parse(source.AsSpan(), _parserOptions);
                        success = true;
                    }
                    catch (SyntaxException ex)
                    {
                        // if we have syntax errors we will report them within the
                        // parse document diagnostic span.
                        var error = context.ErrorHandler.Handle(
                            ErrorBuilder.New()
                                .SetMessage(ex.Message)
                                .SetCode(ErrorCodes.Execution.SyntaxError)
                                .AddLocation(ex.Line, ex.Column)
                                .Build());

                        context.Exception = ex;
                        context.Result = QueryResultBuilder.CreateError(error);

                        _diagnosticEvents.SyntaxError(context, error);
                    }
                }
            }
            else
            {
                throw ThrowHelper.QueryTypeNotSupported();
            }

            if (success)
            {
                await _next(context).ConfigureAwait(false);
            }
        }
        else
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private string ComputeDocumentHash(string? documentHash, string? queryHash, IQuery query)
    {
        return documentHash ?? queryHash ?? _documentHashProvider.ComputeHash(query.AsSpan());
    }
}
