using System;
using HotChocolate.Execution;

namespace HotChocolate.Server
{
    public interface IQueryRequestFactory
    {
        IReadOnlyQueryRequest CreateAsync(
            Action<IQueryRequestBuilder> build);
    }
}
