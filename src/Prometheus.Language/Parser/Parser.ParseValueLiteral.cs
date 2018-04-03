using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Language
{
    public partial class Parser
    {
        private IValueNode ParseValueLiteral(IParserContext context, bool isConstant)
        {
            Token start = context.Current;

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

        private ListValueNode ParseList(IParserContext context, bool isConstant)
        {
            Token start = context.Current;
            IValueNode[] items = (isConstant
                ? ParseMany(context, TokenKind.LeftBracket,
                    ParseConstantValue, TokenKind.RightBracket)
                : ParseMany(context, TokenKind.LeftBracket,
                    c => ParseValueLiteral(c, false), TokenKind.RightBracket)).ToArray();
            Location location = context.CreateLocation(start);

            return new ListValueNode
            (
                location,
                items
            );
        }

        private ObjectValueNode ParseObject(IParserContext context, bool isConstant)
        {
            Token start = context.Current;
            ObjectFieldNode[] fields = ParseMany(context,
                TokenKind.LeftBrace,
                c => ParseObjectField(c, isConstant),
                TokenKind.RightBrace).ToArray();
            Location location = context.CreateLocation(start);

            return new ObjectValueNode
            (
                location,
                fields
            );
        }

        private ObjectFieldNode ParseObjectField(IParserContext context, bool isConstant)
        {
            Token start = context.Current;
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

        private IValueNode ParseScalarValue(IParserContext context)
        {
            Token start = context.ExpectScalarValue();
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

            if (start.IsString())
            {
                return ParseStringLiteral(context);
            }

            throw context.Unexpected(start);
        }

        private IValueNode ParseEnumValue(IParserContext context)
        {
            Token start = context.Current;
            Location location = context.CreateLocation(start);
            context.MoveNext();

            if (start.IsBooleanValue())
            {
                return new BooleanValueNode
                (
                    location,
                    start.Value == Keywords.True
                );
            }

            if (start.Value == Keywords.Null)
            {
                return new NullValueNode(location);
            }

            return new EnumValueNode
            (
                location,
                start.Value
            );
        }

        private IValueNode ParseVariable(IParserContext context)
        {
            Token start = context.ExpectDollar();
            NameNode name = ParseName(context);
            Location location = context.CreateLocation(start);

            return new VariableNode
            (
                location,
                name
            );
        }

        private IValueNode ParseConstantValue(IParserContext context)
        {
            return ParseValueLiteral(context, true);
        }

        private StringValueNode ParseStringLiteral(IParserContext context)
        {
            Token start = context.ExpectString();
            bool isBlock = start.Kind == TokenKind.BlockString;
            Location location = context.CreateLocation(start);

            return new StringValueNode(location, start.Value, isBlock);
        }
    }
}