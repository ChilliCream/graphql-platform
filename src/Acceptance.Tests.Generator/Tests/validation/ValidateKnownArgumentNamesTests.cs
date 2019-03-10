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
    public class ValidateKnownArgumentNames
    {
        private readonly IQueryParser _parser;
        private readonly Schema _schema;
        private readonly ServiceProvider _serviceProvider;
        public ValidateKnownArgumentNames()
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
        public void SingleArgIsKnown()
        {
            // Given
            string query = "fragment argOnRequiredArg on Dog { doesKnowCommand(dogCommand: SIT) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MultipleArgsAreKnown()
        {
            // Given
            string query = "fragment multipleArgs on ComplicatedArgs { multipleReqs(req1: 1, req2: 2) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void IgnoresArgsOfUnknownFields()
        {
            // Given
            string query = "fragment argOnUnknownField on Dog { unknownField(unknownArg: SIT) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MultipleArgsInReverseOrderAreKnown()
        {
            // Given
            string query = "fragment multipleArgsReverseOrder on ComplicatedArgs { multipleReqs(req2: 2, req1: 1) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void NoArgsOnOptionalArg()
        {
            // Given
            string query = "fragment noArgOnOptionalArg on Dog { isHousetrained }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ArgsAreKnownDeeply()
        {
            // Given
            string query = "{ dog { doesKnowCommand(dogCommand: SIT) } human { pet { ... on Dog { doesKnowCommand(dogCommand: SIT) } } } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DirectiveArgsAreKnown()
        {
            // Given
            string query = "{ dog @skip(if: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void UndirectiveArgsAreInvalid()
        {
            // Given
            string query = "{ dog @skip(unless: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirectiveArgument" /*&& arg:argumentName=unless|directiveName=skip, loc:line=2|column=13*/));
        }

        [Fact]
        public void MisspelledDirectiveArgsAreReported()
        {
            // Given
            string query = "{ dog @skip(iff: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownDirectiveArgument" /*&& arg:argumentName=iff|directiveName=skip, loc:line=2|column=13*/));
        }

        [Fact]
        public void InvalidArgName()
        {
            // Given
            string query = "fragment invalidArgName on Dog { doesKnowCommand(unknown: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=unknown|typeName=Dog, loc:line=2|column=19*/));
        }

        [Fact]
        public void MisspelledArgNameIsReported()
        {
            // Given
            string query = "fragment invalidArgName on Dog { doesKnowCommand(dogcommand: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=dogcommand|typeName=Dog, loc:line=2|column=19*/));
        }

        [Fact]
        public void UnknownArgsAmongstKnownArgs()
        {
            // Given
            string query = "fragment oneGoodArgOneInvalidArg on Dog { doesKnowCommand(whoknows: 1, dogCommand: SIT, unknown: true) }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=whoknows|typeName=Dog, loc:line=2|column=19*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=unknown|typeName=Dog, loc:line=2|column=49*/));
        }

        [Fact]
        public void UnknownArgsDeeply()
        {
            // Given
            string query = "{ dog { doesKnowCommand(unknown: true) } human { pet { ... on Dog { doesKnowCommand(unknown: true) } } } }";
            // When
            IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();
            QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));
            // Then
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=unknown|typeName=Dog, loc:line=3|column=21*/));
            Assert.Equal(1, result.Errors.Count(e => e.Code == "unknownArgument" /*&& arg:fieldName=doesKnowCommand|argumentName=unknown|typeName=Dog, loc:line=8|column=25*/));
        }
    }
}
