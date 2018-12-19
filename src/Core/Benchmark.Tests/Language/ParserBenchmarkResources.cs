using System.IO;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    public class ParserBenchmarkResources
    {
        public ParserBenchmarkResources()
        {
            ResourceHelper resources = new ResourceHelper();
            KitchenSinkSchema = new Source(
                resources.GetResourceString("KitchenSinkSchema.graphql"));
            SimpleSchema = new Source(
                resources.GetResourceString("SimpleSchema.graphql"));
            KitchenSinkQuery = new Source(
                resources.GetResourceString("KitchenSinkQuery.graphql"));
            IntrospectionQuery = new Source(
                resources.GetResourceString("IntrospectionQuery.graphql"));
            SimpleQuery = new Source(
                resources.GetResourceString("SimpleQuery.graphql"));
        }

        public ISource KitchenSinkSchema { get; }

        public ISource SimpleSchema { get; }

        public ISource KitchenSinkQuery { get; }

        public ISource IntrospectionQuery { get; }

        public ISource SimpleQuery { get; }
    }
}
