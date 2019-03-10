using HotChocolate.Language;
using HotChocolate.Execution;
using Xunit;

namespace Generated.Tests
{
    public class SchemaParser
    {
        private readonly IQueryParser _parser;
        public SchemaParser()
        {
            _parser = new DefaultQueryParser();
        }

        [Fact]
        public void SimpleType()
        {
            // Given
            string query = "type Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleExtension()
        {
            // Given
            string query = "extend type Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleNonNullType()
        {
            // Given
            string query = "type Hello { world: String! } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleTypeInheritingInterface()
        {
            // Given
            string query = "type Hello implements World";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleTypeInheritingMultipleInterfaces()
        {
            // Given
            string query = "type Hello implements Wo & rld";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SingleValueEnum()
        {
            // Given
            string query = "enum Hello { WORLD }";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void DoubleValueEnum()
        {
            // Given
            string query = "enum Hello { WO, RLD }";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleInterface()
        {
            // Given
            string query = "interface Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleFieldWithArg()
        {
            // Given
            string query = "type Hello { world(flag: Boolean): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleFieldWithArgWithDefaultValue()
        {
            // Given
            string query = "type Hello { world(flag: Boolean = true): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleFieldWithListArg()
        {
            // Given
            string query = "type Hello { world(things: [String]): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleFieldWithTwoArgs()
        {
            // Given
            string query = "type Hello { world(argOne: Boolean, argTwo: Int): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleUnion()
        {
            // Given
            string query = "union Hello = World";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void UnionWithTwoTypes()
        {
            // Given
            string query = "union Hello = Wo | Rld";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Scalar()
        {
            // Given
            string query = "scalar Hello";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleInputObject()
        {
            // Given
            string query = "input Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void SimpleInputObjectWithArgsShouldFail()
        {
            // Given
            string query = "input Hello { world(foo: Int): String } ";
            // Then
            Assert.Throws<SyntaxException>(() =>
            {
                // When
                DocumentNode document = _parser.Parse(query);
            }

            );
        }
    }
}
