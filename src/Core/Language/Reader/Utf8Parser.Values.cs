using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Values section.
    public partial class Utf8Parser
    {
        /// <summary>
        /// Parses a value.
        /// <see cref="IValueNode" />:
        /// - Variable [only if isConstant is <c>false</c>]
        /// - IntValue
        /// - FloatValue
        /// - StringValue
        /// - BooleanValue
        /// - NullValue
        /// - EnumValue
        /// - ListValue[isConstant]
        /// - ObjectValue[isConstant]
        /// <see cref="BooleanValueNode" />: true or false.
        /// <see cref="NullValueNode" />: null
        /// <see cref="EnumValueNode" />: Name but not true, false or null.
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        private static IValueNode ParseValueLiteral(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            if (reader.Kind == TokenKind.LeftBracket)
            {
                return ParseList(context, in reader, isConstant);
            }

            if (reader.Kind == TokenKind.LeftBrace)
            {
                return ParseObject(context, in reader, isConstant);
            }

            if (TokenHelper.IsScalarValue(in reader))
            {
                return ParseScalarValue(context, in reader);
            }

            if (TokenHelper.IsName(in reader))
            {
                return ParseEnumValue(context, in reader);
            }

            if (TokenHelper.IsDollar(in reader) && !isConstant)
            {
                return ParseVariable(context, in reader);
            }

            throw ParserHelper.Unexpected(in reader, reader.Kind);
        }

        private static StringValueNode ParseStringLiteral(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);
            bool isBlock = reader.Kind == TokenKind.BlockString;
            string value = ParserHelper.ExpectString(in reader);
            Location location = context.CreateLocation(in reader);

            return new StringValueNode(location, value, isBlock);
        }

        /// <summary>
        /// Parses a list value.
        /// <see cref="ListValueNode" />:
        /// - [ ]
        /// - [ Value[isConstant]+ ]
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        private static ListValueNode ParseList(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(in reader);

            if (reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(reader)));
            }

            var items = new List<IValueNode>();

            // skip opening token
            reader.Read();

            while (reader.Kind != TokenKind.RightBracket)
            {
                items.Add(ParseValueLiteral(context, in reader, isConstant));
            }

            // skip closing token
            ParserHelper.Expect(in reader, TokenKind.RightBracket);

            Location location = context.CreateLocation(in reader);

            return new ListValueNode
            (
                location,
                items
            );
        }

        /// <summary>
        /// Parses an object value.
        /// <see cref="ObjectValueNode" />:
        /// - { }
        /// - { Value[isConstant]+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        private static ObjectValueNode ParseObject(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(in reader);

            if (reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(reader)));
            }

            var fields = new List<ObjectFieldNode>();

            // skip opening token
            reader.Read();

            while (reader.Kind != TokenKind.RightBrace)
            {
                fields.Add(ParseObjectField(context, in reader, isConstant));
            }

            // skip closing token
            ParserHelper.Expect(in reader, TokenKind.RightBrace);

            Location location = context.CreateLocation(in reader);

            return new ObjectValueNode
            (
                location,
                fields
            );
        }

        private static ObjectFieldNode ParseObjectField(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(in reader);

            NameNode name = ParseName(context, in reader);
            ParserHelper.ExpectColon(in reader);
            IValueNode value = ParseValueLiteral(context, in reader, isConstant);

            Location location = context.CreateLocation(in reader);

            return new ObjectFieldNode
            (
                location,
                name,
                value
            );
        }

        private static IValueNode ParseScalarValue(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsString(in reader))
            {
                return ParseStringLiteral(context, in reader);
            }

            context.Start(in reader);
            TokenKind kind = reader.Kind;
            string value = ParserHelper.ExpectScalarValue(in reader);
            Location location = context.CreateLocation(in reader);

            if (kind == TokenKind.Float)
            {
                return new FloatValueNode
                (
                    location,
                    value
                );
            }

            if (kind == TokenKind.Integer)
            {
                return new IntValueNode
                (
                    location,
                    value
                );
            }

            throw ParserHelper.Unexpected(in reader, kind);
        }

        private static IValueNode ParseEnumValue(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            Location location;

            if (reader.Value.SequenceEqual(Utf8Keywords.True))
            {
                reader.Read();
                location = context.CreateLocation(in reader);
                return new BooleanValueNode(location, true);
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.False))
            {
                reader.Read();
                location = context.CreateLocation(in reader);
                return new BooleanValueNode(location, false);
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.Null))
            {
                reader.Read();
                location = context.CreateLocation(in reader);
                return new NullValueNode(location);
            }

            string value = reader.GetString(reader.Value);
            reader.Read();
            location = context.CreateLocation(in reader);

            return new EnumValueNode
            (
                location,
                value
            );
        }
    }
}
