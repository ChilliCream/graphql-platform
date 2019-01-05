using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public abstract class ValidationTestBase
    {
        public ValidationTestBase(IQueryValidationRule rule)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        protected IQueryValidationRule Rule { get; }

        [Fact]
        public void SchemaIsNulll()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"{ foo }");

            // act
            Action a = () => Rule.Validate(
                null, query, new Dictionary<string, object>());

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();

            // act
            Action a = () => Rule.Validate(
                schema, null, new Dictionary<string, object>());

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void VariableValuesIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"{ foo }");

            // act
            Action a = () => Rule.Validate(
                schema, query, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
