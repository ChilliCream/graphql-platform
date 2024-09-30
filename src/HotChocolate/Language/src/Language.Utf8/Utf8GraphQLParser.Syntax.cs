using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    public static class Syntax
    {
        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static ObjectTypeDefinitionNode ParseObjectTypeDefinition(
            #if NETSTANDARD2_0
            string sourceText) =>
            #else
            [StringSyntax("graphql")] string sourceText) =>
            #endif
            Parse(sourceText, parser => parser.ParseObjectTypeDefinition());

        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static ObjectTypeDefinitionNode ParseObjectTypeDefinition(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseObjectTypeDefinition());

        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static ObjectTypeDefinitionNode ParseObjectTypeDefinition(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseObjectTypeDefinition();

        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static DirectiveDefinitionNode ParseDirectiveDefinition(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseDirectiveDefinition());

        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static DirectiveDefinitionNode ParseDirectiveDefinition(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseDirectiveDefinition());

        /// <summary>
        /// Parses a GraphQL object type definitions e.g. type Foo { bar: String }
        /// </summary>
        public static DirectiveDefinitionNode ParseDirectiveDefinition(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseDirectiveDefinition();

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldDefinitionNode ParseFieldDefinition(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseFieldDefinition());

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldDefinitionNode ParseFieldDefinition(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseFieldDefinition());

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldDefinitionNode ParseFieldDefinition(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseFieldDefinition();

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldNode ParseField(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseField());

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldNode ParseField(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseField());

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FieldNode ParseField(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseField();

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FragmentDefinitionNode ParseFragmentDefinition(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseFragmentDefinition());

        /// <summary>
        /// Parses a GraphQL field selection string e.g. field(arg: "abc")
        /// </summary>
        public static FragmentDefinitionNode ParseFragmentDefinition(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseFragmentDefinition());

        /// <summary>
        /// Parses a GraphQL fragment definition string e.g. fragment Abc on Def { ghi }
        /// </summary>
        public static FragmentDefinitionNode ParseFragmentDefinition(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseFragmentDefinition();

        /// <summary>
        /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
        /// </summary>
        public static SelectionSetNode ParseSelectionSet(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseSelectionSet());

        /// <summary>
        /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
        /// </summary>
        public static SelectionSetNode ParseSelectionSet(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseSelectionSet());

        /// <summary>
        /// Parses a GraphQL selection set string e.g. { field(arg: "abc") }
        /// </summary>
        public static SelectionSetNode ParseSelectionSet(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseSelectionSet();

        public static IValueNode ParseValueLiteral(
#if NETSTANDARD2_0
            string sourceText,
#else
            [StringSyntax("graphql")] string sourceText,
#endif
            bool constant = true) =>
            Parse(sourceText, parser => parser.ParseValueLiteral(constant));

        public static IValueNode ParseValueLiteral(
            ReadOnlySpan<byte> sourceText,
            bool constant = true) =>
            Parse(sourceText, parser => parser.ParseValueLiteral(constant));

        public static IValueNode ParseValueLiteral(
            Utf8GraphQLReader reader,
            bool constant = true) =>
            new Utf8GraphQLParser(reader).ParseValueLiteral(constant);

        public static ObjectValueNode ParseObjectLiteral(
#if NETSTANDARD2_0
            string sourceText,
#else
            [StringSyntax("graphql")] string sourceText,
#endif
            bool constant = true) =>
            Parse(sourceText, parser => parser.ParseObject(constant));

        public static ObjectValueNode ParseObjectLiteral(
            ReadOnlySpan<byte> sourceText,
            bool constant = true) =>
            Parse(sourceText, parser => parser.ParseObject(constant));

        public static ObjectValueNode ParseObjectLiteral(
            Utf8GraphQLReader reader,
            bool constant = true) =>
            new Utf8GraphQLParser(reader).ParseObject(constant);

        /// <summary>
        /// Parses a GraphQL type reference e.g. [String!]
        /// </summary>
        public static ITypeNode ParseTypeReference(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseTypeReference());

        /// <summary>
        /// Parses a GraphQL type reference e.g. [String!]
        /// </summary>
        public static ITypeNode ParseTypeReference(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseTypeReference());

        /// <summary>
        /// Parses a GraphQL type reference e.g. [String!]
        /// </summary>
        public static ITypeNode ParseTypeReference(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseTypeReference();

        /// <summary>
        /// Parses a GraphQL schema coordinate e.g. Query.userById(id:)
        /// </summary>
        public static SchemaCoordinateNode ParseSchemaCoordinate(
#if NETSTANDARD2_0
            string sourceText) =>
#else
            [StringSyntax("graphql")] string sourceText) =>
#endif
            Parse(sourceText, parser => parser.ParseSingleSchemaCoordinate());

        /// <summary>
        /// Parses a GraphQL schema coordinate e.g. Query.userById(id:)
        /// </summary>
        public static SchemaCoordinateNode ParseSchemaCoordinate(
            ReadOnlySpan<byte> sourceText) =>
            Parse(sourceText, parser => parser.ParseSingleSchemaCoordinate());

        /// <summary>
        /// Parses a GraphQL schema coordinate e.g. Query.userById(id:)
        /// </summary>
        public static SchemaCoordinateNode ParseSchemaCoordinate(
            Utf8GraphQLReader reader) =>
            new Utf8GraphQLParser(reader).ParseSchemaCoordinate();

        private static unsafe T Parse<T>(
            string sourceText,
            ParseSyntax<T> parse,
            bool moveNext = true)
            where T : ISyntaxNode
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    SourceText_Empty,
                    nameof(sourceText));
            }

            var length = checked(sourceText.Length * 4);
            byte[]? source = null;

            var sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                ConvertToBytes(sourceText, ref sourceSpan);
                var parser = new Utf8GraphQLParser(sourceSpan);
                if (moveNext)
                {
                    parser.MoveNext();
                }
                return parse(parser);
            }
            finally
            {
                if (source != null)
                {
                    sourceSpan.Clear();
                    ArrayPool<byte>.Shared.Return(source);
                }
            }
        }

        private static T Parse<T>(
            ReadOnlySpan<byte> sourceText,
            ParseSyntax<T> parse,
            bool moveNext = true)
            where T : ISyntaxNode
        {
            var parser = new Utf8GraphQLParser(sourceText);
            if (moveNext)
            {
                parser.MoveNext();
            }
            return parse(parser);
        }
    }

    private delegate T ParseSyntax<out T>(Utf8GraphQLParser parser) where T : ISyntaxNode;
}
