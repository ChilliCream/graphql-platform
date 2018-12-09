using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    // diagnostics -> Exceptions -> Parse -> Validate
    internal sealed class ParseQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryParser _parser;
        private readonly Cache<DocumentNode> _queryCache;

        public ParseQueryMiddleware(
            QueryDelegate next,
            IQueryParser parser,
            Cache<DocumentNode> queryCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _parser = parser
                ?? throw new ArgumentNullException(nameof(parser));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            context.Document = _queryCache.GetOrCreate(
                context.Request.Query,
                () => ParseDocument(context.Request.Query));

            return _next(context);
        }

        private DocumentNode ParseDocument(string queryText)
        {
            return _parser.Parse(queryText);
        }
    }

    internal sealed class DiagnosticMiddleware
    {
        private readonly QueryDelegate _next;

        public DiagnosticMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = QueryDiagnosticEvents.BeginExecute(
                context.Schema, context.Request);

            try
            {
                await _next(context);

                if (context.ValidationResult.HasErrors)
                {
                    QueryDiagnosticEvents.ValidationError(
                        context.Schema, context.Request,
                        context.Document, context.ValidationResult.Errors);
                }

                if (context.Exception != null)
                {
                    QueryDiagnosticEvents.QueryError(
                        context.Schema, context.Request,
                        context.Document, context.Exception);
                }
            }
            finally
            {
                QueryDiagnosticEvents.EndExecute(
                    activity, context.Schema,
                    context.Request, context.Document);
            }
        }
    }

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
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _validator = validator
                ?? throw new ArgumentNullException(nameof(validator));
            _validatorCache = validatorCache
                ?? throw new ArgumentNullException(nameof(validatorCache));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (context.Document == null)
            {
                // TODO : Resources
                throw new QueryException(
                    "The validation pipeline expectes the " +
                    "query document to be parsed.");
            }

            context.ValidationResult = _validatorCache.GetOrCreate(
                context.Request.Query, () => Validate(context.Document));
            return _next(context);
        }

        private QueryValidationResult Validate(DocumentNode document)
        {
            return _validator.Validate(document);
        }
    }

    internal sealed class ExceptionMiddleware
    {
        public readonly QueryDelegate _next;
        public readonly IQueryValidator _validator;

        public ExceptionMiddleware(QueryDelegate next)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            try
            {
                await _next(context);
            }
            catch (QueryException ex)
            {
                context.Exception = ex;
                context.Result = new QueryResult(ex.Errors);
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.Result = new QueryResult(
                    CreateErrorFromException(context.Schema, ex));
            }
        }

        private IError CreateErrorFromException(
            ISchema schema,
            Exception exception)
        {
            if (schema.Options.DeveloperMode)
            {
                return new QueryError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}");
            }
            else
            {
                return new QueryError("Unexpected execution error.");
            }
        }
    }
}

