using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Validation;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    public partial class QueryExecuter
    {
        private readonly ISchema _schema;
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
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _queryValidator = new QueryValidator(schema);
            _queryCache = new Cache<QueryInfo>(cacheSize);
            _operationCache = new Cache<OperationExecuter>(cacheSize * 10);
            _useCache = cacheSize > 0;
            CacheSize = cacheSize;
        }

        public int CacheSize { get; }

        public int CachedQueries => _queryCache.Usage;

        public int CachedOperations => _queryCache.Usage;

        public Task<IExecutionResult> ExecuteAsync(
            QueryRequest queryRequest,
            CancellationToken cancellationToken = default)
        {
            if (queryRequest == null)
            {
                throw new ArgumentNullException(nameof(queryRequest));
            }

            QueryInfo queryInfo = GetOrCreateQuery(queryRequest.Query);
            if (queryInfo.ValidationResult.HasErrors)
            {
                return Task.FromResult<IExecutionResult>(
                    new QueryResult(queryInfo.ValidationResult.Errors));
            }

            return ExecuteInternalAsync(
                queryRequest, queryInfo, cancellationToken);
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
                return new QueryResult(ex.Errors);
            }
            catch (Exception ex)
            {
                return new QueryResult(CreateErrorFromException(ex));
            }
            finally
            {
                operationRequest?.Session.Dispose();
            }
        }

        private IQueryError CreateErrorFromException(Exception exception)
        {
            if (_schema.Options.DeveloperMode)
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
                queryRequest.Services ?? _schema.Services;

            return new OperationRequest(services,
                _schema.Sessions.CreateSession(services))
            {
                VariableValues = queryRequest.VariableValues,
                InitialValue = queryRequest.InitialValue,
            };
        }
    }
}
