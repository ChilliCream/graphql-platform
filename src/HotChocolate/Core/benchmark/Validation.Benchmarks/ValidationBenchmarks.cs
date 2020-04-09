using System;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Configuration;
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
        private readonly IQueryValidator _validator_Old;
        private readonly ISchema _schema;
        private readonly DocumentNode _introspectionQuery;

        public ValidationBenchmarks()
        {
            _services = new ServiceCollection()
                // new
                .AddValidation().Services
                // old
                .AddQueryValidation()
                .AddDefaultValidationRules()
                .AddSingleton<IValidateQueryOptionsAccessor, BenchmarkValidationOptions>()
                // star wars schema
                .AddStarWarsRepositories()
                .AddGraphQLSchema(b => b.AddStarWarsTypes())
                .BuildServiceProvider();

            _validator = _services.GetRequiredService<IDocumentValidator>();
            _validator_Old = _services.GetRequiredService<IQueryValidator>();

            _schema = _services.GetRequiredService<ISchema>();
            var resources = new ResourceHelper();
            _introspectionQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("IntrospectionQuery.graphql"));
        }

        [GlobalSetup]
        public void Setup()
        {
            _validator.Validate(_schema, _introspectionQuery);
            _validator_Old.Validate(_schema, _introspectionQuery);
        }

        [Benchmark]
        public void ValidateIntrospection()
        {
            _validator.Validate(_schema, _introspectionQuery);
        }

        [Benchmark]
        public void ValidateIntrospection_Old()
        {
            _validator_Old.Validate(_schema, _introspectionQuery);
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
