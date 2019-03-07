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
    public class ValidateFieldsOnCorrectType
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateFieldsOnCorrectType()
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
        public void ObjectFieldSelection()
        {
            // Given
            string query = @"fragment objectFieldSelection on Dog { __typename name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void AliasedObjectFieldSelection()
        {
            // Given
            string query = @"fragment aliasedObjectFieldSelection on Dog { tn : __typename otherName : name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void InterfaceFieldSelection()
        {
            // Given
            string query = @"fragment interfaceFieldSelection on Pet { __typename name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void AliasedInterfaceFieldSelection()
        {
            // Given
            string query = @"fragment interfaceFieldSelection on Pet { otherName : name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void LyingAliasSelection()
        {
            // Given
            string query = @"fragment lyingAliasSelection on Dog { name : nickname }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void IgnoresFieldsOnUnknownType()
        {
            // Given
            string query = @"fragment unknownSelection on UnknownType { unknownField }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ReportsErrorsWhenTypeIsKnownAgain()
        {
            // Given
            string query = @"fragment typeKnownAgain on Pet { unknown_pet_field { ... on Cat { unknown_cat_field } } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=unknown_pet_field|type=Pet, loc:line=2|column=3*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=unknown_cat_field|type=Cat, loc:line=4|column=7*/));
        }

        [Fact]
        public void FieldNotDefinedOnFragment()
        {
            // Given
            string query = @"fragment fieldNotDefined on Dog { meowVolume }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=meowVolume|type=Dog, loc:line=2|column=3*/));
        }

        [Fact]
        public void IgnoresDeeplyUnknownField()
        {
            // Given
            string query = @"fragment deepFieldNotDefined on Dog { unknown_field { deeper_unknown_field } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=unknown_field|type=Dog, loc:line=2|column=3*/));
        }

        [Fact]
        public void SubFieldNotDefined()
        {
            // Given
            string query = @"fragment subFieldNotDefined on Human { pets { unknown_field } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=unknown_field|type=Pet, loc:line=3|column=5*/));
        }

        [Fact]
        public void FieldNotDefinedOnInlineFragment()
        {
            // Given
            string query = @"fragment fieldNotDefined on Pet { ... on Dog { meowVolume } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=meowVolume|type=Dog, loc:line=3|column=5*/));
        }

        [Fact]
        public void AliasedFieldTargetNotDefined()
        {
            // Given
            string query = @"fragment aliasedFieldTargetNotDefined on Dog { volume : mooVolume }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=mooVolume|type=Dog, loc:line=2|column=3*/));
        }

        [Fact]
        public void AliasedLyingFieldTargetNotDefined()
        {
            // Given
            string query = @"fragment aliasedLyingFieldTargetNotDefined on Dog { barkVolume : kawVolume }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=kawVolume|type=Dog, loc:line=2|column=3*/));
        }

        [Fact]
        public void NotDefinedOnInterface()
        {
            // Given
            string query = @"fragment notDefinedOnInterface on Pet { tailLength }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=tailLength|type=Pet, loc:line=2|column=3*/));
        }

        [Fact]
        public void DefinedOnImplementorsButNotOnInterface()
        {
            // Given
            string query = @"fragment definedOnImplementorsButNotInterface on Pet { nickname }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=nickname|type=Pet, loc:line=2|column=3*/));
        }

        [Fact]
        public void MetaFieldSelectionOnUnion()
        {
            // Given
            string query = @"fragment directFieldSelectionOnUnion on CatOrDog { __typename }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DirectFieldSelectionOnUnion()
        {
            // Given
            string query = @"fragment directFieldSelectionOnUnion on CatOrDog { directField }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=directField|type=CatOrDog, loc:line=2|column=3*/));
        }

        [Fact]
        public void DefinedOnImplementorsQueriedOnUnion()
        {
            // Given
            string query = @"fragment definedOnImplementorsQueriedOnUnion on CatOrDog { name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "undefinedField" /*&& arg:fieldName=name|type=CatOrDog, loc:line=2|column=3*/));
        }

        [Fact]
        public void ValidFieldInInlineFragment()
        {
            // Given
            string query = @"fragment objectFieldSelection on Pet { ... on Dog { name } ... { name } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }
    }
}
