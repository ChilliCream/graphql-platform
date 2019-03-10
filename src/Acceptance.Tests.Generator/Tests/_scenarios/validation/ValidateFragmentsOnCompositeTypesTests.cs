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
    public class ValidateFragmentsOnCompositeTypes
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateFragmentsOnCompositeTypes()
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
        public void ObjectIsValidFragmentType()
        {
            // Given
            string query = "fragment validFragment on Dog { barks }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void InterfaceIsValidFragmentType()
        {
            // Given
            string query = "fragment validFragment on Pet { name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ObjectIsValidInlineFragmentType()
        {
            // Given
            string query = "fragment validFragment on Pet { ... on Dog { barks } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void InlineFragmentWithoutTypeIsValid()
        {
            // Given
            string query = "fragment validFragment on Pet { ... { name } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void UnionIsValidFragmentType()
        {
            // Given
            string query = "fragment validFragment on CatOrDog { __typename }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ScalarIsInvalidFragmentType()
        {
            // Given
            string query = "fragment scalarFragment on Boolean { bad }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "fragmentOnNonCompositeType" /*&& arg:type=Boolean|fragmentName=scalarFragment, loc:line=1|column=28*/));
        }

        [Fact]
        public void EnumIsInvalidFragmentType()
        {
            // Given
            string query = "fragment scalarFragment on FurColor { bad }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "fragmentOnNonCompositeType" /*&& arg:type=FurColor|fragmentName=scalarFragment, loc:line=1|column=28*/));
        }

        [Fact]
        public void InputObjectIsInvalidFragmentType()
        {
            // Given
            string query = "fragment inputFragment on ComplexInput { stringField }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "fragmentOnNonCompositeType" /*&& arg:type=ComplexInput|fragmentName=inputFragment, loc:line=1|column=27*/));
        }

        [Fact]
        public void ScalarIsInvalidInlineFragmentType()
        {
            // Given
            string query = "fragment invalidFragment on Pet { ... on String { barks } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "inlineFragmentOnNonCompositeType" /*&& arg:type=String, loc:line=2|column=10*/));
        }
    }
}
