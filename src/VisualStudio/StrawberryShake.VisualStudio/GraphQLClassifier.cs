using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using StrawberryShake.VisualStudio.Language;

namespace StrawberryShake.VisualStudio
{

    internal sealed class GraphQLClassifier : IClassifier
    {
        private readonly ITextBuffer _buffer;
        private readonly IGraphQLClassificationService _classifications;
        private DocumentNode _document;

        public GraphQLClassifier(ITextBuffer buffer, IGraphQLClassificationService classifications)
        {
            _buffer = buffer;
            _classifications = classifications;
            _buffer.Changed += _buffer_Changed;
            ParseDocument();
        }

        private void _buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            ParseDocument();
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();
            TokenKind? previous = null;

            Debug.WriteLine(span.GetText());
            Debug.WriteLine(span.Start);
            Debug.WriteLine(span.Length);


            try
            {
                var reader = new StringGraphQLParser(Encoding.UTF8.GetBytes(span.GetText()));

                while (reader.Read())
                {
                    if (reader.Kind != TokenKind.StartOfFile && reader.Kind != TokenKind.EndOfFile)
                    {
                        switch (reader.Kind)
                        {
                            case TokenKind.Comment:
                                list.AddClassification(span, reader, _classifications.Comment);
                                break;

                            case TokenKind.Name:
                                if (previous.IsKeywordPossible() && reader.Value.IsKeyword())
                                {
                                    list.AddClassification(span, reader, _classifications.Keyword);
                                }
                                else
                                {
                                    list.AddClassification(span, reader, _classifications.Identifier);
                                }
                                break;

                            case TokenKind.Float:
                            case TokenKind.Integer:
                                list.AddClassification(span, reader, _classifications.NumberLiteral);
                                break;

                            case TokenKind.String:
                            case TokenKind.BlockString:
                                list.AddClassification(span, reader, _classifications.StringLiteral);
                                break;

                            default:
                                list.AddClassification(span, reader, _classifications.Other);
                                break;
                        }
                    }
                    previous = reader.Kind;
                }
            }
            catch (SyntaxException)
            {
            }

            return list;
        }

        private void ParseDocument()
        {
            try
            {
                _document = Utf8GraphQLParser.Parse(_buffer.CurrentSnapshot.GetText());
            }
            catch (SyntaxException)
            {
            }
        }
    }

    internal static class ListExtensions
    {
        public static void AddClassification(
            this ICollection<ClassificationSpan> classifications,
            SnapshotSpan snapshotSpan,
            Utf8GraphQLReader reader,
            IClassificationType type)
        {
            var start = snapshotSpan.Start.Position + reader.Start;
            var length = reader.End - reader.Start;
            var span = new Span(start, length);

            classifications.Add(new ClassificationSpan(
                new SnapshotSpan(snapshotSpan.Snapshot, span),
                type));
        }
    }

    internal static class GraphQLKeywords
    {
        // type system
        public static readonly byte[] Schema = new byte[]
        {
            (byte)'s',
            (byte)'c',
            (byte)'h',
            (byte)'e',
            (byte)'m',
            (byte)'a'
        };

        public static readonly byte[] Scalar = new byte[]
        {
            (byte)'s',
            (byte)'c',
            (byte)'a',
            (byte)'l',
            (byte)'a',
            (byte)'r'
        };

        public static readonly byte[] Type = new byte[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

        public static readonly byte[] Interface = new byte[]
        {
            (byte)'i',
            (byte)'n',
            (byte)'t',
            (byte)'e',
            (byte)'r',
            (byte)'f',
            (byte)'a',
            (byte)'c',
            (byte)'e'
        };

        public static readonly byte[] Union = new byte[]
        {
            (byte)'u',
            (byte)'n',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Enum = new byte[]
        {
            (byte)'e',
            (byte)'n',
            (byte)'u',
            (byte)'m'
        };

        public static readonly byte[] Input = new byte[]
        {
            (byte)'i',
            (byte)'n',
            (byte)'p',
            (byte)'u',
            (byte)'t'
        };

        public static readonly byte[] Extend = new byte[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'d'
        };

        public static readonly byte[] Implements = new byte[]
        {
            (byte)'i',
            (byte)'m',
            (byte)'p',
            (byte)'l',
            (byte)'e',
            (byte)'m',
            (byte)'e',
            (byte)'n',
            (byte)'t',
            (byte)'s'
        };

        public static readonly byte[] Repeatable = new byte[]
        {
            (byte)'r',
            (byte)'e',
            (byte)'p',
            (byte)'e',
            (byte)'a',
            (byte)'t',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e'
        };

        public static readonly byte[] Directive = new byte[]
        {
            (byte)'d',
            (byte)'i',
            (byte)'r',
            (byte)'e',
            (byte)'c',
            (byte)'t',
            (byte)'i',
            (byte)'v',
            (byte)'e'
        };

        // query
        public static readonly byte[] Query = new byte[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        public static readonly byte[] Mutation = new byte[]
        {
            (byte)'m',
            (byte)'u',
            (byte)'t',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Subscription = new byte[]
        {
            (byte)'s',
            (byte)'u',
            (byte)'b',
            (byte)'s',
            (byte)'c',
            (byte)'r',
            (byte)'i',
            (byte)'p',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] Fragment = new byte[]
        {
            (byte)'f',
            (byte)'r',
            (byte)'a',
            (byte)'g',
            (byte)'m',
            (byte)'e',
            (byte)'n',
            (byte)'t'
        };

        // general
        public static readonly byte[] On = new byte[]
        {
            (byte)'o',
            (byte)'n'
        };

        public static readonly byte[] True = new byte[]
        {
            (byte)'t',
            (byte)'r',
            (byte)'u',
            (byte)'e'
        };

        public static readonly byte[] False = new byte[]
        {
            (byte)'f',
            (byte)'a',
            (byte)'l',
            (byte)'s',
            (byte)'e'
        };

        public static readonly byte[] Null = new byte[]
        {
            (byte)'n',
            (byte)'u',
            (byte)'l',
            (byte)'l'
        };

        public static bool IsKeyword(this ReadOnlySpan<byte> value)
        {
            return value.SequenceEqual(Schema)
                || value.SequenceEqual(Scalar)
                || value.SequenceEqual(Type)
                || value.SequenceEqual(Interface)
                || value.SequenceEqual(Union)
                || value.SequenceEqual(Enum)
                || value.SequenceEqual(Input)
                || value.SequenceEqual(Extend)
                || value.SequenceEqual(Implements)
                || value.SequenceEqual(Repeatable)
                || value.SequenceEqual(Directive)
                || value.SequenceEqual(Query)
                || value.SequenceEqual(Mutation)
                || value.SequenceEqual(Subscription)
                || value.SequenceEqual(Fragment)
                || value.SequenceEqual(On)
                || value.SequenceEqual(True)
                || value.SequenceEqual(False)
                || value.SequenceEqual(Null);
        }

        public static bool IsKeywordPossible(this TokenKind? previous)
        {
            if (previous.HasValue
                && (previous.Value == TokenKind.LeftParenthesis
                    || previous.Value == TokenKind.Dollar))
            {
                return false;
            }
            return true;
        }
    }
}
