using System.Collections.Generic;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Values section.
    public partial class Parser
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
        internal static IValueNode ParseValueLiteral(
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;

            if (start.Kind == TokenKind.LeftBracket)
            {
                return ParseList(context, isConstant);
            }

            if (start.Kind == TokenKind.LeftBrace)
            {
                return ParseObject(context, isConstant);
            }

            if (start.IsScalarValue())
            {
                return ParseScalarValue(context);
            }

            if (start.IsName())
            {
                return ParseEnumValue(context);
            }

            if (start.IsDollar() && !isConstant)
            {
                return ParseVariable(context);
            }

            throw context.Unexpected(start);
        }

        private static StringValueNode ParseStringLiteral(
            ParserContext context)
        {
            SyntaxToken start = context.ExpectString();
            bool isBlock = start.Kind == TokenKind.BlockString;
            Location location = context.CreateLocation(start);

            return new StringValueNode(location, start.Value, isBlock);
        }

        private static IValueNode ParseConstantValue(ParserContext context)
        {
            return ParseValueLiteral(context, true);
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
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;
            List<IValueNode> items = isConstant
                ? ParseMany(context, TokenKind.LeftBracket,
                    ParseConstantValue, TokenKind.RightBracket)
                : ParseMany(context, TokenKind.LeftBracket,
                    c => ParseValueLiteral(c, false), TokenKind.RightBracket);
            Location location = context.CreateLocation(start);

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
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;
            List<ObjectFieldNode> fields = ParseMany(context,
                TokenKind.LeftBrace,
                c => ParseObjectField(c, isConstant),
                TokenKind.RightBrace);
            Location location = context.CreateLocation(start);

            return new ObjectValueNode
            (
                location,
                fields
            );
        }

        private static ObjectFieldNode ParseObjectField(
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;
            NameNode name = ParseName(context);
            context.ExpectColon();
            IValueNode value = ParseValueLiteral(context, isConstant);
            Location location = context.CreateLocation(start);

            return new ObjectFieldNode
            (
                location,
                name,
                value
            );
        }

        private static IValueNode ParseScalarValue(ParserContext context)
        {
            if (context.Current.IsString())
            {
                return ParseStringLiteral(context);
            }

            SyntaxToken start = context.ExpectScalarValue();
            Location location = context.CreateLocation(start);

            if (start.Kind == TokenKind.Float)
            {
                return new FloatValueNode
                (
                    location,
                    start.Value
                );
            }

            if (start.Kind == TokenKind.Integer)
            {
                return new IntValueNode
                (
                    location,
                    start.Value
                );
            }

            throw context.Unexpected(start);
        }

        private static IValueNode ParseEnumValue(ParserContext context)
        {
            SyntaxToken start = context.Current;
            Location location = context.CreateLocation(start);
            context.MoveNext();

            switch (start.Value)
            {
                case Keywords.True:
                    return new BooleanValueNode(location, true);
                case Keywords.False:
                    return new BooleanValueNode(location, false);
                case Keywords.Null:
                    return new NullValueNode(location);
            }

            return new EnumValueNode
            (
                location,
                start.Value
            );
        }
    }
}
