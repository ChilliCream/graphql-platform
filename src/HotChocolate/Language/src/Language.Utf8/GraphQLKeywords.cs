using System;

namespace HotChocolate.Language;

internal static class GraphQLKeywords
{
    // type system
    public static ReadOnlySpan<byte> Schema => "schema"u8;

    public static ReadOnlySpan<byte> Scalar => "scalar"u8;

    public static ReadOnlySpan<byte> Type => "type"u8;

    public static ReadOnlySpan<byte> Interface => "interface"u8;

    public static ReadOnlySpan<byte> Union => "union"u8;

    public static ReadOnlySpan<byte> Enum => "enum"u8;

    public static ReadOnlySpan<byte> Input => "input"u8;

    public static ReadOnlySpan<byte> Extend => "extend"u8;

    public static ReadOnlySpan<byte> Implements => "implements"u8;

    public static ReadOnlySpan<byte> Repeatable => "repeatable"u8;

    public static ReadOnlySpan<byte> Directive => "directive"u8;

    // query
    public static ReadOnlySpan<byte> Query => "query"u8;

    public static ReadOnlySpan<byte> Mutation => "mutation"u8;

    public static ReadOnlySpan<byte> Subscription => "subscription"u8;

    public static ReadOnlySpan<byte> Fragment => "fragment"u8;

    // general
    public static ReadOnlySpan<byte> On => "on"u8;

    public static ReadOnlySpan<byte> True => "true"u8;

    public static ReadOnlySpan<byte> False => "false"u8;

    public static ReadOnlySpan<byte> Null => "null"u8;
}
