using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8GraphQLParserSyntaxTests
    {
        [Fact]
        public void Parse_FieldNode_From_String() =>
            Utf8GraphQLParser.Syntax.ParseField(@"foo(a: ""baz"")").MatchSnapshot();

        [Fact]
        public void Parse_FieldNode_From_ByteArray() =>
            Utf8GraphQLParser.Syntax.ParseField(GetUtf8Bytes(@"foo(a: ""baz"")")).MatchSnapshot();

        [Fact]
        public void Parse_FieldNode_From_Reader()
        {
            var reader = new Utf8GraphQLReader(GetUtf8Bytes(@"foo(a: ""baz"")"));
            reader.MoveNext();
            
            Utf8GraphQLParser.Syntax.ParseField(reader).MatchSnapshot();
        }

        [Fact]
        public void Parse_SelectionSetNode_From_String() =>
            Utf8GraphQLParser.Syntax.ParseSelectionSet(@"{ foo(a: ""baz"") }").MatchSnapshot();

        [Fact]
        public void Parse_SelectionSetNode_From_ByteArray() =>
            Utf8GraphQLParser.Syntax.ParseSelectionSet(GetUtf8Bytes(@"{ foo(a: ""baz"") }")).MatchSnapshot();

        [Fact]
        public void Parse_SelectionSetNode_From_Reader()
        {
            var reader = new Utf8GraphQLReader(GetUtf8Bytes(@"{ foo(a: ""baz"") }"));
            reader.MoveNext();
            
            Utf8GraphQLParser.Syntax.ParseSelectionSet(reader).MatchSnapshot();
        }

        [Fact]
        public void Parse_ValueNode_From_String() =>
            Utf8GraphQLParser.Syntax.ParseValueLiteral(@"""baz""").MatchSnapshot();

        [Fact]
        public void Parse_ValueNode_From_ByteArray() =>
            Utf8GraphQLParser.Syntax.ParseValueLiteral(GetUtf8Bytes(@"""baz""")).MatchSnapshot();

        [Fact]
        public void Parse_ValueNode_From_Reader()
        {
            var reader = new Utf8GraphQLReader(GetUtf8Bytes(@"""baz"""));
            reader.MoveNext();
            
            Utf8GraphQLParser.Syntax.ParseValueLiteral(reader).MatchSnapshot();
        }

        [Fact]
        public void Parse_ObjectValueNode_From_String() =>
            Utf8GraphQLParser.Syntax.ParseObjectLiteral(@"{ a: 1 }").MatchSnapshot();

        [Fact]
        public void Parse_ObjectValueNode_From_ByteArray() =>
            Utf8GraphQLParser.Syntax.ParseObjectLiteral(GetUtf8Bytes(@"{ a: 1 }")).MatchSnapshot();

        [Fact]
        public void Parse_ObjectValueNode_From_Reader()
        {
            var reader = new Utf8GraphQLReader(GetUtf8Bytes(@"{ a: 1 }"));
            reader.MoveNext();
            
            Utf8GraphQLParser.Syntax.ParseObjectLiteral(reader).MatchSnapshot();
        }

        private byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s);

    }
}
