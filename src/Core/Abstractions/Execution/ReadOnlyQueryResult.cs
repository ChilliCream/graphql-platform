using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class ReadOnlyQueryResult
        : IReadOnlyQueryResult
    {
        private readonly IReadOnlyQueryResult _queryResult;

        public ReadOnlyQueryResult(IReadOnlyQueryResult queryResult)
        {
            _queryResult = queryResult
                ?? throw new ArgumentNullException(nameof(queryResult));
        }

        public IReadOnlyDictionary<string, object> Data =>
            _queryResult.Data;

        public IReadOnlyDictionary<string, object> Extensions =>
            _queryResult.Extensions;

        public IReadOnlyCollection<IError> Errors =>
            _queryResult.Errors;

        public IReadOnlyDictionary<string, object> ContextData =>
            _queryResult.ContextData;

        public IReadOnlyDictionary<string, object> ToDictionary() =>
            _queryResult.ToDictionary();
    }
}
