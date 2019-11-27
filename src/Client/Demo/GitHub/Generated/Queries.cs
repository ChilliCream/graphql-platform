using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public class Queries
        : IDocument
    {
        private readonly byte[] _hashName = new byte[]
        {
            109,
            100,
            53,
            72,
            97,
            115,
            104
        };
        private readonly byte[] _hash = new byte[]
        {
            90,
            53,
            85,
            76,
            50,
            88,
            83,
            104,
            52,
            79,
            115,
            121,
            65,
            53,
            114,
            110,
            53,
            81,
            114,
            86,
            79,
            103,
            61,
            61
        };
        private readonly byte[] _content = new byte[]
        {
            113,
            117,
            101,
            114,
            121,
            32,
            103,
            101,
            116,
            85,
            115,
            101,
            114,
            40,
            36,
            108,
            111,
            103,
            105,
            110,
            58,
            32,
            83,
            116,
            114,
            105,
            110,
            103,
            33,
            41,
            32,
            123,
            32,
            95,
            95,
            116,
            121,
            112,
            101,
            110,
            97,
            109,
            101,
            32,
            117,
            115,
            101,
            114,
            40,
            108,
            111,
            103,
            105,
            110,
            58,
            32,
            36,
            108,
            111,
            103,
            105,
            110,
            41,
            32,
            123,
            32,
            95,
            95,
            116,
            121,
            112,
            101,
            110,
            97,
            109,
            101,
            32,
            110,
            97,
            109,
            101,
            32,
            99,
            111,
            109,
            112,
            97,
            110,
            121,
            32,
            99,
            114,
            101,
            97,
            116,
            101,
            100,
            65,
            116,
            32,
            102,
            111,
            108,
            108,
            111,
            119,
            101,
            114,
            115,
            32,
            123,
            32,
            95,
            95,
            116,
            121,
            112,
            101,
            110,
            97,
            109,
            101,
            32,
            116,
            111,
            116,
            97,
            108,
            67,
            111,
            117,
            110,
            116,
            32,
            125,
            32,
            125,
            32,
            125
        };

        public ReadOnlySpan<byte> HashName => _hashName;

        public ReadOnlySpan<byte> Hash => _hash;

        public ReadOnlySpan<byte> Content => _content;

        public static Queries Default { get; } = new Queries();

        public override string ToString() => 
            @"query getUser($login: String!) {
              user(login: $login) {
                name
                company
                createdAt
                followers {
                  totalCount
                }
              }
            }";
    }
}
