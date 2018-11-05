using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Validation;
using HotChocolate.Runtime;
using System.Diagnostics;

namespace HotChocolate.Execution
{
    public partial class QueryExecuter
    {
        private readonly QueryValidator _queryValidator;
        private readonly Cache<QueryInfo> _queryCache;
        private readonly Cache<OperationExecuter> _operationCache;
        private readonly bool _useCache;

        public QueryExecuter(ISchema schema)
            : this(schema, 100)
        {
        }

        public QueryExecuter(ISchema schema, int cacheSize)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _queryValidator = new QueryValidator(schema);
            _queryCache = new Cache<QueryInfo>(cacheSize);
            _operationCache = new Cache<OperationExecuter>(cacheSize * 10);
            _useCache = cacheSize > 0;
            CacheSize = cacheSize;
        }

        public ISchema Schema { get; }

        public int CacheSize { get; }

        public int CachedQueries => _queryCache.Usage;

        public int CachedOperations => _queryCache.Usage;

        public Task<IExecutionResult> ExecuteAsync(
            QueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            QueryInfo queryInfo = null;
            Activity activity = QueryDiagnosticEvents.BeginExecute(
                Schema,
                request);

            try
            {
                queryInfo = GetOrCreateQuery(request.Query);
                if (queryInfo.ValidationResult.HasErrors)
                {
                    QueryDiagnosticEvents.ValidationError(
                        Schema,
                        request,
                        queryInfo.QueryDocument,
                        queryInfo.ValidationResult.Errors);

                    return Task.FromResult<IExecutionResult>(
                        new QueryResult(queryInfo.ValidationResult.Errors));
                }

                return ExecuteInternalAsync(
                    request, queryInfo, cancellationToken);
            }
            finally
            {
                QueryDiagnosticEvents.EndExecute(
                    activity,
                    Schema,
                    request,
                    queryInfo?.QueryDocument);
            }
        }

        private async Task<IExecutionResult> ExecuteInternalAsync(
            QueryRequest queryRequest,
            QueryInfo queryInfo,
            CancellationToken cancellationToken)
        {
            OperationRequest operationRequest = null;
            try
            {
                OperationExecuter operationExecuter =
                    GetOrCreateOperationExecuter(
                        queryRequest, queryInfo.QueryDocument);

                operationRequest =
                    CreateOperationRequest(queryRequest);

                return await operationExecuter.ExecuteAsync(
                    operationRequest, cancellationToken);
            }
            catch (QueryException ex)
            {
                QueryDiagnosticEvents.QueryError(
                    Schema,
                    queryRequest,
                    queryInfo.QueryDocument,
                    ex);

                return new QueryResult(ex.Errors);
            }
            catch (Exception ex)
            {
                QueryDiagnosticEvents.QueryError(
                    Schema,
                    queryRequest,
                    queryInfo.QueryDocument,
                    ex);

                return new QueryResult(CreateErrorFromException(ex));
            }
            finally
            {
                operationRequest?.Session.Dispose();
            }
        }

        private IQueryError CreateErrorFromException(Exception exception)
        {
            if (Schema.Options.DeveloperMode)
            {
                return new QueryError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}");
            }
            else
            {
                return new QueryError("Unexpected execution error.");
            }
        }

        private OperationRequest CreateOperationRequest(
            QueryRequest queryRequest)
        {
            IServiceProvider services =
                queryRequest.Services ?? Schema.Services;

            return new OperationRequest(services,
                Schema.Sessions.CreateSession(services))
            {
                VariableValues = queryRequest.VariableValues,
                Properties = queryRequest.Properties,
                InitialValue = queryRequest.InitialValue,
            };
        }
    }
}
