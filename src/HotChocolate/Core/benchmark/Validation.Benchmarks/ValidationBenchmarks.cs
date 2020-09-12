using System;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.StarWars;

namespace HotChocolate.Validation.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ValidationBenchmarks
            : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IDocumentValidator _validator;
        private readonly ISchema _schema;
        private readonly DocumentNode _introspectionQuery;
        private readonly DocumentNode _starWarsQuery;

        public ValidationBenchmarks()
        {
            _services = new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsTypes()
                .Services
                .AddStarWarsRepositories()
                .BuildServiceProvider();

            var factory = _services.GetRequiredService<IDocumentValidatorFactory>();
            _validator = factory.CreateValidator();

            _schema = _services.GetRequiredService<ISchema>();
            var resources = new ResourceHelper();
            _introspectionQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("IntrospectionQuery.graphql"));
            _starWarsQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("StarWarsQuery.graphql"));
        }

        [GlobalSetup]
        public void Setup()
        {
            _validator.Validate(_schema, _introspectionQuery);
        }

        [Benchmark]
        public void ValidateIntrospection()
        {
            _validator.Validate(_schema, _introspectionQuery);
        }

        [Benchmark]
        public void ValidateStarWars()
        {
            _validator.Validate(_schema, _introspectionQuery);
        }

        public void Dispose()
        {
            if (_services is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
