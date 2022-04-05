using System;

namespace HotChocolate.Language;

internal static class GraphQLKeywords
{
    // type system
    public static ReadOnlySpan<byte> Schema => new[]
    {
        (byte)'s',
        (byte)'c',
        (byte)'h',
        (byte)'e',
        (byte)'m',
        (byte)'a'
    };

    public static ReadOnlySpan<byte> Scalar => new[]
    {
        (byte)'s',
        (byte)'c',
        (byte)'a',
        (byte)'l',
        (byte)'a',
        (byte)'r'
    };

    public static ReadOnlySpan<byte> Type => new[]
    {
        (byte)'t',
        (byte)'y',
        (byte)'p',
        (byte)'e'
    };

    public static ReadOnlySpan<byte> Interface => new[]
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

    public static ReadOnlySpan<byte> Union => new[]
    {
        (byte)'u',
        (byte)'n',
        (byte)'i',
        (byte)'o',
        (byte)'n'
    };

    public static ReadOnlySpan<byte> Enum => new[]
    {
        (byte)'e',
        (byte)'n',
        (byte)'u',
        (byte)'m'
    };

    public static ReadOnlySpan<byte> Input => new[]
    {
        (byte)'i',
        (byte)'n',
        (byte)'p',
        (byte)'u',
        (byte)'t'
    };

    public static ReadOnlySpan<byte> Extend => new[]
    {
        (byte)'e',
        (byte)'x',
        (byte)'t',
        (byte)'e',
        (byte)'n',
        (byte)'d'
    };

    public static ReadOnlySpan<byte> Implements => new[]
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

    public static ReadOnlySpan<byte> Repeatable => new[]
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

    public static ReadOnlySpan<byte> Directive => new[]
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
    public static ReadOnlySpan<byte> Query => new[]
    {
        (byte)'q',
        (byte)'u',
        (byte)'e',
        (byte)'r',
        (byte)'y'
    };

    public static ReadOnlySpan<byte> Mutation => new[]
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

    public static ReadOnlySpan<byte> Subscription => new[]
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

    public static ReadOnlySpan<byte> Fragment => new[]
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
    public static ReadOnlySpan<byte> On => new[]
    {
        (byte)'o',
        (byte)'n'
    };

    public static ReadOnlySpan<byte> True => new[]
    {
        (byte)'t',
        (byte)'r',
        (byte)'u',
        (byte)'e'
    };

    public static ReadOnlySpan<byte> False => new[]
    {
        (byte)'f',
        (byte)'a',
        (byte)'l',
        (byte)'s',
        (byte)'e'
    };

    public static ReadOnlySpan<byte> Null => new[]
    {
        (byte)'n',
        (byte)'u',
        (byte)'l',
        (byte)'l'
    };
}
