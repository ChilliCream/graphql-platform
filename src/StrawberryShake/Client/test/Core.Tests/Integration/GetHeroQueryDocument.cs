using System;
using System.Text;

namespace StrawberryShake.Integration
{
    public class GetHeroQueryDocument : IDocument
    {
        private const string BodyString =
            @"query GetHero {
                hero {
                    __typename
                    id
                    name
                    friends {
                        nodes {
                            __typename
                            id
                            name
                        }
                        totalCount
                    }
                }
                version
            }";

        private static readonly byte[] s_body = Encoding.UTF8.GetBytes(BodyString);

        private GetHeroQueryDocument() { }

        public static GetHeroQueryDocument Instance { get; } = new();

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => s_body;

        public DocumentHash Hash { get; } = new("MD5", "ABC");

        public override string ToString() => BodyString;
    }
}
