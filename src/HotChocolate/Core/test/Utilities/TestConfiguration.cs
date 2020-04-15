using System;
using HotChocolate.Execution;

namespace HotChocolate.Tests
{
    public class TestConfiguration
    {
        public ISchema? Schema { get; set; }
        public IQueryExecutor? Executor { get; set; }
        public IServiceProvider? Service { get; set; }
        public Action<IQueryRequestBuilder>? ModifyRequest { get; set; }
    }
}
