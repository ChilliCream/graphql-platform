using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class QueryResult : IReadOnlyQueryResult
    {
        private readonly IDisposable? _disposable;

        public QueryResult(
            IReadOnlyDictionary<string, object?>? data,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extension = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IDisposable? disposable = null)
        {
            if (data is null && errors is null)
            {
                throw new ArgumentException(
                    "data and errors cannot be null at the same time.",
                    nameof(data));
            }

            Data = data;
            Errors = errors;
            Extensions = extension;
            ContextData = contextData;
            _disposable = disposable;
        }

        public IReadOnlyDictionary<string, object?>? Data { get; }

        public IReadOnlyList<IError>? Errors { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public IReadOnlyDictionary<string, object?>? ContextData { get; }

        public IReadOnlyDictionary<string, object?> ToDictionary()
        {
            return QueryResultHelper.ToDictionary(this);
        }

        public void Dispose()
        {
            if (Data is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_disposable is { })
            {
                _disposable.Dispose();
            }
        }
    }
}
