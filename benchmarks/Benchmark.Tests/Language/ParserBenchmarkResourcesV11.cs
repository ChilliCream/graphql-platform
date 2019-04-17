using System.Text;

namespace HotChocolate.Benchmark.Tests.Language
{
    public class ParserBenchmarkResourcesV11
    {
        public ParserBenchmarkResourcesV11()
        {
            ResourceHelper resources = new ResourceHelper();
            KitchenSinkSchema = Encoding.UTF8.GetBytes(
                resources.GetResourceString("KitchenSinkSchema.graphql"));
            SimpleSchema = Encoding.UTF8.GetBytes(
                resources.GetResourceString("SimpleSchema.graphql"));
            KitchenSinkQuery = Encoding.UTF8.GetBytes(
                resources.GetResourceString("KitchenSinkQuery.graphql"));
            IntrospectionQuery = Encoding.UTF8.GetBytes(
                resources.GetResourceString("IntrospectionQuery.graphql"));
            SimpleQuery = Encoding.UTF8.GetBytes(
                resources.GetResourceString("SimpleQuery.graphql"));
        }

        public byte[] KitchenSinkSchema { get; }

        public byte[] SimpleSchema { get; }

        public byte[] KitchenSinkQuery { get; }

        public byte[] IntrospectionQuery { get; }

        public byte[] SimpleQuery { get; }
    }
}
