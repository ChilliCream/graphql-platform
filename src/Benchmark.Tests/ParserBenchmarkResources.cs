using System.IO;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests
{
    public class ParserBenchmarkResources
    {
        public ParserBenchmarkResources()
        {
            KitchenSinkSchema = new Source(File.ReadAllText("KitchenSinkSchema.graphql"));
            SimpleSchema = new Source(File.ReadAllText("SimpleSchema.graphql"));
            KitchenSinkQuery = new Source(File.ReadAllText("KitchenSinkQuery.graphql"));
            IntrospectionQuery = new Source(File.ReadAllText("IntrospectionQuery.graphql"));
            SimpleQuery = new Source(File.ReadAllText("SimpleQuery.graphql"));
        }

        public ISource KitchenSinkSchema { get; }

        public ISource SimpleSchema { get; }

        public ISource KitchenSinkQuery { get; }

        public ISource IntrospectionQuery { get; }

        public ISource SimpleQuery { get; }
    }
}
