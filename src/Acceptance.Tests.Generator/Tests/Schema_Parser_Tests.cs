using HotChocolate.Language;
using HotChocolate.Execution;
using Xunit;

namespace Generated.Tests
{
    public class Schema_Parser
    {
        private readonly IQueryParser _parser;
        public Schema_Parser()
        {
            _parser = new DefaultQueryParser();
        }

        [Fact]
        public void Simple_type()
        {
            // Given
            string query = @"type Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_extension()
        {
            // Given
            string query = @"extend type Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_nonnull_type()
        {
            // Given
            string query = @"type Hello { world: String! } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_type_inheriting_interface()
        {
            // Given
            string query = @"type Hello implements World";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_type_inheriting_multiple_interfaces()
        {
            // Given
            string query = @"type Hello implements Wo & rld";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Single_value_enum()
        {
            // Given
            string query = @"enum Hello { WORLD }";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Double_value_enum()
        {
            // Given
            string query = @"enum Hello { WO, RLD }";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_interface()
        {
            // Given
            string query = @"interface Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_field_with_arg()
        {
            // Given
            string query = @"type Hello { world(flag: Boolean): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_field_with_arg_with_default_value()
        {
            // Given
            string query = @"type Hello { world(flag: Boolean = true): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_field_with_list_arg()
        {
            // Given
            string query = @"type Hello { world(things: [String]): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_field_with_two_args()
        {
            // Given
            string query = @"type Hello { world(argOne: Boolean, argTwo: Int): String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_union()
        {
            // Given
            string query = @"union Hello = World";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Union_with_two_types()
        {
            // Given
            string query = @"union Hello = Wo | Rld";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Scalar()
        {
            // Given
            string query = @"scalar Hello";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_input_object()
        {
            // Given
            string query = @"input Hello { world: String } ";
            // When
            DocumentNode document = _parser.Parse(query);
            // Then
            Assert.NotNull(document);
        }

        [Fact]
        public void Simple_input_object_with_args_should_fail()
        {
            // Given
            string query = @"input Hello { world(foo: Int): String } ";
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