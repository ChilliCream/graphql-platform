using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Runtime.CompilerServices;

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
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            if (reader.Kind == TokenKind.LeftBracket)
            {
                return ParseList(context, ref reader, isConstant);
            }

            if (reader.Kind == TokenKind.LeftBrace)
            {
                return ParseObject(context, ref reader, isConstant);
            }

            if (TokenHelper.IsScalarValue(ref reader))
            {
                return ParseScalarValue(context, ref reader);
            }

            if (TokenHelper.IsName(ref reader))
            {
                return ParseEnumValue(context, ref reader);
            }

            if (TokenHelper.IsDollar(ref reader) && !isConstant)
            {
                return ParseVariable(context, ref reader);
            }

            throw ParserHelper.Unexpected(ref reader, reader.Kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StringValueNode ParseStringLiteral(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);
            bool isBlock = reader.Kind == TokenKind.BlockString;
            string value = ParserHelper.ExpectString(ref reader);
            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ListValueNode ParseList(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(ref reader);

            if (reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in reader)));
            }

            var items = new List<IValueNode>();

            // skip opening token
            ParserHelper.MoveNext(ref reader);

            while (reader.Kind != TokenKind.RightBracket)
            {
                items.Add(ParseValueLiteral(context, ref reader, isConstant));
            }

            // skip closing token
            ParserHelper.Expect(ref reader, TokenKind.RightBracket);

            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectValueNode ParseObject(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(ref reader);

            if (reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in reader)));
            }

            var fields = new List<ObjectFieldNode>();

            // skip opening token
            ParserHelper.MoveNext(ref reader);

            while (reader.Kind != TokenKind.RightBrace)
            {
                fields.Add(ParseObjectField(context, ref reader, isConstant));
            }

            // skip closing token
            ParserHelper.Expect(ref reader, TokenKind.RightBrace);

            Location location = context.CreateLocation(ref reader);

            return new ObjectValueNode
            (
                location,
                fields
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectFieldNode ParseObjectField(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(ref reader);

            NameNode name = ParseName(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            IValueNode value = ParseValueLiteral(context, ref reader, isConstant);

            Location location = context.CreateLocation(ref reader);

            return new ObjectFieldNode
            (
                location,
                name,
                value
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IValueNode ParseScalarValue(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsString(ref reader))
            {
                return ParseStringLiteral(context, ref reader);
            }

            context.Start(ref reader);
            TokenKind kind = reader.Kind;
            string value = ParserHelper.ExpectScalarValue(ref reader);
            Location location = context.CreateLocation(ref reader);

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

            throw ParserHelper.Unexpected(ref reader, kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IValueNode ParseEnumValue(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            Location location;

            if (reader.Value.SequenceEqual(Utf8Keywords.True))
            {
                ParserHelper.MoveNext(ref reader);
                location = context.CreateLocation(ref reader);
                return new BooleanValueNode(location, true);
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.False))
            {
                ParserHelper.MoveNext(ref reader);
                location = context.CreateLocation(ref reader);
                return new BooleanValueNode(location, false);
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.Null))
            {
                ParserHelper.MoveNext(ref reader);
                location = context.CreateLocation(ref reader);
                return new NullValueNode(location);
            }

            string value = reader.GetString(reader.Value);
            ParserHelper.MoveNext(ref reader);
            location = context.CreateLocation(ref reader);

            return new EnumValueNode
            (
                location,
                value
            );
        }
    }
}
