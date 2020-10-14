using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.StarWars;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class FieldCollectorBenchmarks
    {
        private readonly ISchema _schema;
        private readonly DocumentNode _introspectionQuery;
        private readonly FragmentCollection _introspectionFragments;
        private readonly OperationDefinitionNode _introspectionOperation;
        private readonly DocumentNode _starWarsQuery;
        private readonly FragmentCollection _starWarsFragments;
        private readonly OperationDefinitionNode _starWarsOperation;

        public FieldCollectorBenchmarks()
        {
            var resources = new ResourceHelper();
            
            _schema = SchemaBuilder.New().AddStarWarsTypes().Create();
            
            _introspectionQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("IntrospectionQuery.graphql"));
            _introspectionOperation = (OperationDefinitionNode)_introspectionQuery.Definitions[0];
            
            _starWarsQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("GetTwoHerosWithFriendsQuery.graphql"));
            _starWarsOperation = (OperationDefinitionNode)_starWarsQuery.Definitions[0];
        }

        [Benchmark]
        public object PrepareSelectionSets_Introspection()
        {
            return OperationCompiler.Compile(
                "a",
                _introspectionQuery,
                _introspectionOperation,
                _schema,
                _schema.QueryType);
        }

        [Benchmark]
        public object PrepareSelectionSets_StarWars()
        {
            return OperationCompiler.Compile(
                "b",
                _starWarsQuery,
                _starWarsOperation,
                _schema,
                _schema.QueryType);
        }
    }
}
