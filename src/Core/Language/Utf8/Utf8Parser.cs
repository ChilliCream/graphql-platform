using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed partial class Utf8Parser
    {
        public DocumentNode Parse(
            ReadOnlySpan<byte> source,
            ParserOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (source.Length == 0)
            {
                throw new ArgumentException(nameof(source));
            }

            var context = new Utf8ParserContext(options);
            var reader = new Utf8GraphQLReader(source);

            return ParseDocument(context, ref reader);
        }

        private static DocumentNode ParseDocument(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            var definitions = new List<IDefinitionNode>();

            context.Start(ref reader);

            ParserHelper.MoveNext(ref reader);

            while (reader.Kind != TokenKind.EndOfFile)
            {
                definitions.Add(ParseDefinition(context, ref reader));
            }

            Location location = context.CreateLocation(ref reader);

            return new DocumentNode(location, definitions);
        }

        private static IDefinitionNode ParseDefinition(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsDescription(ref reader))
            {
                context.PushDescription(ParseDescription(context, ref reader));
            }

            if (reader.Kind == TokenKind.Name)
            {
                if (reader.Value.SequenceEqual(Utf8Keywords.Query)
                    || reader.Value.SequenceEqual(Utf8Keywords.Mutation)
                    || reader.Value.SequenceEqual(Utf8Keywords.Subscription))
                {
                    return ParseOperationDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Fragment))
                {
                    return ParseFragmentDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Schema))
                {
                    ParseSchemaDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Scalar))
                {
                    return ParseScalarTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Type))
                {
                    return ParseObjectTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Interface))
                {
                    return ParseInterfaceTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Union))
                {
                    return ParseUnionTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Enum))
                {
                    return ParseEnumTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Input))
                {
                    return ParseInputObjectTypeDefinition(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Extend))
                {
                    return ParseTypeExtension(context, ref reader);
                }

                if (reader.Value.SequenceEqual(Utf8Keywords.Directive))
                {
                    return ParseDirectiveDefinition(context, ref reader);
                }
            }
            else if (reader.Kind == TokenKind.LeftBrace)
            {
                return ParseOperationDefinitionShortHandForm(
                    context, ref reader);
            }

            throw ParserHelper.Unexpected(ref reader, reader.Kind);
        }

        public static Parser Default { get; } = new Parser();
    }
}
