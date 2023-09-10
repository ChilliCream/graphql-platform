using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Execution;

namespace HotChocolate.Validation.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ValidationBenchmarks : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IDocumentValidator _validator;
        private readonly ISchema _schema;
        private readonly DocumentNode _introspectionQuery;
        private readonly DocumentNode _starWarsQuery;
        private readonly Dictionary<string, object> _contextData = new();

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

            _schema = _services.GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result.Schema;
            var resources = new ResourceHelper();
            _introspectionQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("IntrospectionQuery.graphql"));
            _starWarsQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("StarWarsQuery.graphql"));
        }

        [GlobalSetup]
        public async Task Setup()
        {
            await _validator.ValidateAsync(
                    _schema,
                    _introspectionQuery,
                    "abc",
                    _contextData,
                    false)
                .ConfigureAwait(false);
        }

        [Benchmark]
        public async Task ValidateIntrospection()
        {
            await _validator.ValidateAsync(
                    _schema,
                    _introspectionQuery,
                    "abc",
                    _contextData,
                    false)
                .ConfigureAwait(false);
        }

        [Benchmark]
        public async Task ValidateStarWars()
        {
            await _validator.ValidateAsync(
                    _schema,
                    _starWarsQuery,
                    "abc",
                    _contextData,
                    false)
                .ConfigureAwait(false);
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
