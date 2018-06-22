using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryExecuter
    {
        private readonly object _sync = new object();
        private readonly Schema _schema;
        private readonly int _size = 100;
        private readonly LinkedList<string> _requestRanking = new LinkedList<string>();
        private ImmutableDictionary<string, CachedRequest> _cachedRequests =
            ImmutableDictionary<string, CachedRequest>.Empty;
        private LinkedListNode<string> _first;

        public QueryExecuter(Schema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public QueryExecuter(Schema schema, int cacheSize)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _size = cacheSize < 10 ? 10 : cacheSize;
        }

        public async Task<QueryResult> ExecuteAsync(
            QueryRequest queryRequest,
            CancellationToken cancellationToken = default)
        {
            if (queryRequest == null)
            {
                throw new ArgumentNullException(nameof(queryRequest));
            }

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
                operationRequest = AddNewEntry(operationKey, queryRequest);
            }

            return await operationRequest.ExecuteAsync(
                queryRequest.VariableValues, queryRequest.InitialValue,
                cancellationToken);
        }

        private string CreateOperationKey(string query, string operationName)
        {
            // normalize query
            string normalizedQuery = query
                .Replace("\r\n", string.Empty)
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

        private OperationRequest AddNewEntry(string operationKey, QueryRequest queryRequest)
        {
            DocumentNode queryDocument = Parser.Default.Parse(queryRequest.Query);
            OperationRequest request = new OperationRequest(
                _schema, queryDocument, queryRequest.OperationName);

            if (!_cachedRequests.ContainsKey(operationKey))
            {
                lock (_sync)
                {
                    if (!_cachedRequests.ContainsKey(operationKey))
                    {
                        ClearSpaceForNewEntry();
                        LinkedListNode<string> rank = _requestRanking.AddFirst(operationKey);
                        CachedRequest entry = new CachedRequest(rank, request);
                        _cachedRequests = _cachedRequests.SetItem(operationKey, entry);
                        _first = rank;
                    }
                }
            }

            return request;
        }

        private void ClearSpaceForNewEntry()
        {
            if (_cachedRequests.Count >= _size)
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
