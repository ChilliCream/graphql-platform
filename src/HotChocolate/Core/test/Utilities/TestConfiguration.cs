using System;
using HotChocolate.Execution;

namespace HotChocolate.Tests
{
    public class TestConfiguration
    {
        public Action<ISchemaBuilder>? CreateSchema { get; set; }
        public Func<ISchema, IQueryExecutor>? CreateExecutor { get; set; }
        public IServiceProvider? Service { get; set; }
        public Action<IQueryRequestBuilder>? ModifyRequest { get; set; }
    }
}
