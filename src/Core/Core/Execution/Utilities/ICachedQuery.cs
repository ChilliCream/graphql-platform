using System;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface ICachedQuery
    {
        DocumentNode Document { get; }

        IPreparedOperation GetOrCreate(string operationId, Func<IPreparedOperation> create);
    }
}
