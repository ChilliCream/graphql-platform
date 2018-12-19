using System;
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
            Action a = () => Rule.Validate(null, query);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();

            // act
            Action a = () => Rule.Validate(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
