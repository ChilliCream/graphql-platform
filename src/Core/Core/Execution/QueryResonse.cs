using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal class QueryResonse
        : IQueryResponse
    {
        public OrderedDictionary Data { get; } =
            new OrderedDictionary();

        public IDictionary<string, object> Extensions { get; } =
            new ConcurrentDictionary<string, object>();

        public ICollection<IError> Errors { get; } =
            new ErrorCollection();
    }
}
