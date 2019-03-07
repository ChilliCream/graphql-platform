using System;
using System.IO;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Generated.Tests
{
    public class ValidateExecutableDefinitions
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateExecutableDefinitions()
        {
            _parser = new DefaultQueryParser();
            var schemaContent = File.ReadAllText("validation.schema.graphql");
            _schema = Schema.Create(schemaContent, c => c.Use(next => context => throw new NotImplementedException()));
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDefaultValidationRules();
            serviceCollection.AddQueryValidation();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void WithOnlyOperation()
        {
            // Given
            string query = @"query Foo { dog { name } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithOperationAndFragment()
        {
            // Given
            string query = @"query Foo { dog { name ...Frag } } fragment Frag on Dog { name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithTypeDefinition()
        {
            // Given
            string query = @"query Foo { dog { name } } type Cow { name: String } extend type Dog { color: String }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "nonExecutableDefinition" /*&& arg:defName=Cow, loc:line=7|column=1*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "nonExecutableDefinition" /*&& arg:defName=Dog, loc:line=11|column=1*/));
        }

        [Fact]
        public void WithSchemaDefinition()
        {
            // Given
            string query = @"schema { query: Query } type Query { test: String } extend schema @directive";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(3, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "nonExecutableDefinition" /*&& arg:defName=schema, loc:line=1|column=1*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "nonExecutableDefinition" /*&& arg:defName=Query, loc:line=5|column=1*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "nonExecutableDefinition" /*&& arg:defName=schema, loc:line=9|column=1*/));
        }
    }
}
