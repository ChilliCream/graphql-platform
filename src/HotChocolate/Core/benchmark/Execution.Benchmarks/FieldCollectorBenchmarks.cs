using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Utilities;
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
            _introspectionFragments = new FragmentCollection(_schema, _introspectionQuery);
            _introspectionOperation = (OperationDefinitionNode)_introspectionQuery.Definitions[0];
            
            _starWarsQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("GetTwoHerosWithFriendsQuery.graphql"));
            _starWarsFragments = new FragmentCollection(_schema, _starWarsQuery);
            _starWarsOperation = (OperationDefinitionNode)_starWarsQuery.Definitions[0];
        }

        [Benchmark]
        public object PrepareSelectionSets_Introspection()
        {
            return OperationCompiler.Compile(
                _schema,
                _introspectionFragments,
                _introspectionOperation);
        }

        [Benchmark]
        public object PrepareSelectionSets_StarWars()
        {
            return OperationCompiler.Compile(
                _schema,
                _starWarsFragments,
                _starWarsOperation);
        }
    }
}
