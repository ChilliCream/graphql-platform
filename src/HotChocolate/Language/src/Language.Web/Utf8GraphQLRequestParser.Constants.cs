using System;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const byte _o = (byte)'o';
    private const byte _q = (byte)'q';
    private const byte _v = (byte)'v';
    private const byte _e = (byte)'e';
    private const byte _t = (byte)'t';
    private const byte _i = (byte)'i';
    private const byte _p = (byte)'p';

    private static ReadOnlySpan<byte> Id => "id"u8;
}
