using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryExecuter
    {
        private readonly object _sync = new object();
        private readonly Schema _schema;
        private readonly LinkedList<string> _requestRanking = new LinkedList<string>();
        private ImmutableDictionary<string, CachedRequest> _cachedRequests =
            ImmutableDictionary<string, CachedRequest>.Empty;
        private LinkedListNode<string> _first;

        public QueryExecuter(Schema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            CacheSize = 100;
        }

        public QueryExecuter(Schema schema, int cacheSize)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            CacheSize = cacheSize < 0 ? 0 : cacheSize;
        }

        public int CacheSize { get; }
        public int CachedOperations => _cachedRequests.Count;

        public async Task<QueryResult> ExecuteAsync(
            QueryRequest queryRequest,
            CancellationToken cancellationToken = default)
        {
            if (queryRequest == null)
            {
                throw new ArgumentNullException(nameof(queryRequest));
            }

            try
            {
                OperationRequest operationRequest =
                    GetOrCreateRequest(queryRequest);

                return await operationRequest.ExecuteAsync(
                    queryRequest.VariableValues, queryRequest.InitialValue,
                    cancellationToken);
            }
            catch (QueryException ex)
            {
                return new QueryResult(ex.Errors);
            }
            catch (Exception ex)
            {
                return new QueryResult(_schema.CreateErrorFromException(ex));
            }
        }

        private static OperationDefinitionNode GetOperation(
            DocumentNode queryDocument, string operationName)
        {
            OperationDefinitionNode[] operations = queryDocument.Definitions
                .OfType<OperationDefinitionNode>()
                .ToArray();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Length == 1)
                {
                    return operations[0];
                }

                throw new QueryException(
                    "Only queries that contain one operation can be executed " +
                    "without specifying the opartion name.");
            }
            else
            {
                OperationDefinitionNode operation = operations.SingleOrDefault(
                    t => string.Equals(t.Name.Value, operationName, StringComparison.Ordinal));
                if (operation == null)
                {
                    throw new QueryException(
                        $"The specified operation `{operationName}` does not exist.");
                }
                return operation;
            }
        }

        private OperationRequest GetOrCreateRequest(QueryRequest queryRequest)
        {
            if (CacheSize == 0)
            {
                return CreateOperationRequest(queryRequest);
            }
            return GetOrCreateRequestFromCache(queryRequest);
        }

        private OperationRequest GetOrCreateRequestFromCache(QueryRequest queryRequest)
        {
            string operationKey = CreateOperationKey(
              queryRequest.Query, queryRequest.OperationName);

            OperationRequest operationRequest;
            if (_cachedRequests.TryGetValue(operationKey, out CachedRequest entry))
            {
                TouchEntry(entry.Rank);
                operationRequest = entry.Request;
            }
            else
            {
                operationRequest = CreateOperationRequest(queryRequest);
                AddNewEntry(operationKey, operationRequest);
            }
            return operationRequest;
        }

        private string CreateOperationKey(string query, string operationName)
        {
            // normalize query
            string normalizedQuery = query
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            if (operationName == null)
            {
                return normalizedQuery;
            }
            return $"{operationName}++{query}";
        }

        private void TouchEntry(LinkedListNode<string> rank)
        {
            if (rank != _first)
            {
                lock (_sync)
                {
                    if (_requestRanking.First != rank)
                    {
                        _requestRanking.Remove(rank);
                        _requestRanking.AddFirst(rank);
                        _first = rank;
                    }
                }
            }
        }

        private void AddNewEntry(string operationKey, OperationRequest operationRequest)
        {
            if (!_cachedRequests.ContainsKey(operationKey))
            {
                lock (_sync)
                {
                    if (!_cachedRequests.ContainsKey(operationKey))
                    {
                        ClearSpaceForNewEntry();
                        LinkedListNode<string> rank = _requestRanking.AddFirst(operationKey);
                        CachedRequest entry = new CachedRequest(rank, operationRequest);
                        _cachedRequests = _cachedRequests.SetItem(operationKey, entry);
                        _first = rank;
                    }
                }
            }
        }

        private OperationRequest CreateOperationRequest(QueryRequest queryRequest)
        {
            DocumentNode queryDocument = Parser.Default.Parse(queryRequest.Query);
            return new OperationRequest(
                _schema, queryDocument, GetOperation(
                    queryDocument, queryRequest.OperationName));
        }

        private void ClearSpaceForNewEntry()
        {
            if (_cachedRequests.Count >= CacheSize)
            {
                LinkedListNode<string> entry = _requestRanking.Last;
                _cachedRequests = _cachedRequests.Remove(entry.Value);
                _requestRanking.Remove(entry);
            }
        }

        private class CachedRequest
        {
            public CachedRequest(
                LinkedListNode<string> rank,
                OperationRequest request)
            {
                Rank = rank
                    ?? throw new System.ArgumentNullException(nameof(rank));
                Request = request
                    ?? throw new System.ArgumentNullException(nameof(request));
            }

            public LinkedListNode<string> Rank { get; }
            public OperationRequest Request { get; }
        }
    }
}
