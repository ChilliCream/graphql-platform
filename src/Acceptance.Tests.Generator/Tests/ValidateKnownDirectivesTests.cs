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
    public class ValidateKnownDirectives
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateKnownDirectives()
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
        public void WithNoDirectives()
        {
            // Given
            string query = "query Foo { name ...Frag } fragment Frag on Dog { name }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithKnownDirectives()
        {
            // Given
            string query = "{ dog @include(if: true) { name } human @skip(if: false) { name } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithUnknownDirective()
        {
            // Given
            string query = "{ dog @unknown(directive: \"value\") { name } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirective" /*&& arg:directiveName=unknown, loc:line=2|column=7*/));
        }

        [Fact]
        public void WithManyUnknownDirectives()
        {
            // Given
            string query = "{ dog @unknown(directive: \"value\") { name } human @unknown(directive: \"value\") { name pets @unknown(directive: \"value\") { name } } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(3, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirective" /*&& arg:directiveName=unknown, loc:line=2|column=7*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirective" /*&& arg:directiveName=unknown, loc:line=5|column=9*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirective" /*&& arg:directiveName=unknown, loc:line=7|column=10*/));
        }

        [Fact]
        public void WithWellPlacedDirectives()
        {
            // Given
            string query = "query Foo @onQuery { name @include(if: true) ...Frag @include(if: true) skippedField @skip(if: true) ...SkippedFrag @skip(if: true) } mutation Bar @onMutation { someField }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithMisplacedDirectives()
        {
            // Given
            string query = "query Foo @include(if: true) { name @onQuery ...Frag @onQuery } mutation Bar @onQuery { someField }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(4, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=include|location=QUERY, loc:line=1|column=11*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onQuery|location=FIELD, loc:line=2|column=8*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onQuery|location=FRAGMENT_SPREAD, loc:line=3|column=11*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onQuery|location=MUTATION, loc:line=6|column=14*/));
        }

        [Fact]
        public void WithinSchemaLanguageWithWellPlacedDirectives()
        {
            // Given
            string query = "type MyObj implements MyInterface @onObject { myField(myArg: Int @onArgumentDefinition): String @onFieldDefinition } extend type MyObj @onObject scalar MyScalar @onScalar extend scalar MyScalar @onScalar interface MyInterface @onInterface { myField(myArg: Int @onArgumentDefinition): String @onFieldDefinition } extend interface MyInterface @onInterface union MyUnion @onUnion = MyObj | Other extend union MyUnion @onUnion enum MyEnum @onEnum { MY_VALUE @onEnumValue } extend enum MyEnum @onEnum input MyInput @onInputObject { myField: Int @onInputFieldDefinition } extend input MyInput @onInputObject schema @onSchema { query: MyQuery } extend schema @onSchema";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void WithinSchemaLanguageWithMisplacedDirectives()
        {
            // Given
            string query = "type MyObj implements MyInterface @onInterface { myField(myArg: Int @onInputFieldDefinition): String @onInputFieldDefinition } scalar MyScalar @onEnum interface MyInterface @onObject { myField(myArg: Int @onInputFieldDefinition): String @onInputFieldDefinition } union MyUnion @onEnumValue = MyObj | Other enum MyEnum @onScalar { MY_VALUE @onUnion } input MyInput @onEnum { myField: Int @onArgumentDefinition } schema @onObject { query: MyQuery } extend schema @onObject";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(14, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onInterface|location=OBJECT, loc:line=1|column=35*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onInputFieldDefinition|location=ARGUMENT_DEFINITION, loc:line=2|column=22*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onInputFieldDefinition|location=FIELD_DEFINITION, loc:line=2|column=55*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onEnum|location=SCALAR, loc:line=5|column=17*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onObject|location=INTERFACE, loc:line=7|column=23*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onInputFieldDefinition|location=ARGUMENT_DEFINITION, loc:line=8|column=22*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onInputFieldDefinition|location=FIELD_DEFINITION, loc:line=8|column=55*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onEnumValue|location=UNION, loc:line=11|column=15*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onScalar|location=ENUM, loc:line=13|column=13*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onUnion|location=ENUM_VALUE, loc:line=14|column=12*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onEnum|location=INPUT_OBJECT, loc:line=17|column=15*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onArgumentDefinition|location=INPUT_FIELD_DEFINITION, loc:line=18|column=16*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onObject|location=SCHEMA, loc:line=21|column=8*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "misplacedDirective" /*&& arg:directiveName=onObject|location=SCHEMA, loc:line=25|column=15*/));
        }
    }
}
