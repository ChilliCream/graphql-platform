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
    public class ValidateScalarLeafs
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateScalarLeafs()
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
        public void ValidScalarSelection()
        {
            // Given
            string query = "fragment scalarSelection on Dog { barks } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ObjectTypeMissingSelection()
        {
            // Given
            string query = "query directQueryOnObjectWithoutSubFields { human } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "requiredSubselection" /*&& arg:fieldName=human|type=Human, loc:line=2|column=3*/));
        }

        [Fact]
        public void InterfaceTypeMissingSelection()
        {
            // Given
            string query = "{ human { pets } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "requiredSubselection" /*&& arg:fieldName=pets|type=[Pet], loc:line=2|column=11*/));
        }

        [Fact]
        public void ValidScalarSelectionWithArgs()
        {
            // Given
            string query = "fragment scalarSelectionWithArgs on Dog { doesKnowCommand(dogCommand: SIT) } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ScalarSelectionNotAllowedOnBoolean()
        {
            // Given
            string query = "fragment scalarSelectionsNotAllowedOnBoolean on Dog { barks { sinceWhen } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "noSubselectionAllowed" /*&& arg:fieldName=barks|type=Boolean, loc:line=2|column=3*/));
        }

        [Fact]
        public void ScalarSelectionNotAllowedOnEnum()
        {
            // Given
            string query = "fragment scalarSelectionsNotAllowedOnEnum on Cat { furColor { inHexdec } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "noSubselectionAllowed" /*&& arg:fieldName=furColor|type=FurColor, loc:line=2|column=3*/));
        }

        [Fact]
        public void ScalarSelectionNotAllowedWithArgs()
        {
            // Given
            string query = "fragment scalarSelectionsNotAllowedWithArgs on Dog { doesKnowCommand(dogCommand: SIT) { sinceWhen } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "noSubselectionAllowed" /*&& arg:fieldName=doesKnowCommand|type=Boolean, loc:line=2|column=3*/));
        }

        [Fact]
        public void ScalarSelectionNotAllowedWithDirectives()
        {
            // Given
            string query = "fragment scalarSelectionsNotAllowedWithDirectives on Dog { name @include(if: true) { isAlsoHumanName } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "noSubselectionAllowed" /*&& arg:fieldName=name|type=String, loc:line=2|column=3*/));
        }

        [Fact]
        public void ScalarSelectionNotAllowedWithDirectivesAndArgs()
        {
            // Given
            string query = "fragment scalarSelectionsNotAllowedWithDirectivesAndArgs on Dog { doesKnowCommand(dogCommand: SIT) @include(if: true) { sinceWhen } } ";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "noSubselectionAllowed" /*&& arg:fieldName=doesKnowCommand|type=Boolean, loc:line=2|column=3*/));
        }
    }
}
