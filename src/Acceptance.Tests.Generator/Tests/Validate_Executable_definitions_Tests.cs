using System;
using System.Collections.Generic;
using System.IO;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Generated.Tests
{
    public class Validate_Executable_definitions
    {
        private IQueryParser _parser;
        private Schema _schema;
        private ServiceProvider _serviceProvider;
        public Validate_Executable_definitions()
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
        public void with_only_operation()
        {
            // Given
            string query = @"query Foo {   dog {     name   } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void with_operation_and_fragment()
        {
            // Given
            string query = @"query Foo {   dog {     name     ...Frag   } }  fragment Frag on Dog {   name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void with_type_definition()
        {
            // Given
            string query = @"query Foo {   dog {     name   } }  type Cow {   name: String }  extend type Dog {   color: String }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
        }

        [Fact]
        public void with_schema_definition()
        {
            // Given
            string query = @"schema {   query: Query }  type Query {   test: String }  extend schema @directive";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
            throw new NotImplementedException();
        }
    }
}
