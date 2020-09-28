using Xunit;

namespace HotChocolate.Language
{
    public class SyntaxParserHelperTests
    {
        [Fact]
        public static void ParseField()
        {
            FieldNode field = Utf8GraphQLParser.Syntax.ParseField("foo");
            Assert.Equal("foo", field.Name.Value);
        }

        [Fact]
        public static void ParseCompositeField()
        {
            FieldNode field = Utf8GraphQLParser.Syntax.ParseField("foo { bar }");
            Assert.Equal("foo", field.Name.Value);
        }

        [Fact]
        public static void ParseSelectionSet()
        {
            SelectionSetNode selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ bar }");
            Assert.Collection(
                selectionSet.Selections,
                field => Assert.Equal("bar",
                    Assert.IsType<FieldNode>(field).Name.Value));
        }

        [Fact]
        public static void ParseValueLiteral()
        {
            IValueNode literal = Utf8GraphQLParser.Syntax.ParseValueLiteral("BAZ");
            Assert.IsType<EnumValueNode>(literal);
        }

        [Fact]
        public static void ParseVariable()
        {
            IValueNode literal = Utf8GraphQLParser.Syntax.ParseValueLiteral("$foo", false);
            Assert.IsType<VariableNode>(literal);
        }
    }
}
