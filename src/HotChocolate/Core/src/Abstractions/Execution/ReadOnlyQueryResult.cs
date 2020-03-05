using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class ReadOnlyQueryResult
        : IReadOnlyQueryResult
    {
        public ReadOnlyQueryResult(
            IReadOnlyDictionary<string, object>? data,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object>? extension,
            IReadOnlyDictionary<string, object>? contextData)
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
        }

        public IReadOnlyDictionary<string, object>? Data { get; }

        public IReadOnlyList<IError>? Errors { get; }

        public IReadOnlyDictionary<string, object>? Extensions { get; }

        public IReadOnlyDictionary<string, object>? ContextData { get; }

        public IReadOnlyDictionary<string, object> ToDictionary()
        {
            return QueryResultHelper.ToDictionary(this);
        }

        public void Dispose()
        {
            if (Data is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
