#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

/// <summary>
/// Provides string interning for well-known GraphQL names to avoid
/// repeated UTF-8 to string conversions for common identifiers.
/// </summary>
internal static class WellKnownNames
{
    // Built-in scalar type names
    private const string String = "String";
    private const string Int = "Int";
    private const string Float = "Float";
    private const string Boolean = "Boolean";
    private const string ID = "ID";

    // Operation type names
    private const string Query = "Query";
    private const string Mutation = "Mutation";
    private const string Subscription = "Subscription";

    // Introspection names
#pragma warning disable IDE1006 // Naming Styles
    private const string __typename = "__typename";
    private const string __schema = "__schema";
    private const string __type = "__type";
    private const string __Type = "__Type";
    private const string __Field = "__Field";
    private const string __InputValue = "__InputValue";
    private const string __EnumValue = "__EnumValue";
    private const string __Directive = "__Directive";
    private const string __Schema = "__Schema";
#pragma warning restore IDE1006 // Naming Styles

    // Common field/argument names
    private const string Id = "id";
    private const string Name = "name";
    private const string Description = "description";
    private const string Type = "type";
    private const string Kind = "kind";
    private const string Fields = "fields";
    private const string Args = "args";
    private const string Node = "node";
    private const string Nodes = "nodes";
    private const string Edges = "edges";
    private const string Cursor = "cursor";
    private const string First = "first";
    private const string Last = "last";
    private const string After = "after";
    private const string Before = "before";
    private const string Value = "value";
    private const string Key = "key";
    private const string Input = "input";
    private const string Where = "where";
    private const string Order = "order";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetWellKnownName(
        ReadOnlySpan<byte> utf8Value,
#if NETSTANDARD2_0
        out string? result)
#else
        [NotNullWhen(true)] out string? result)
#endif
    {
        if (utf8Value.Length == 0)
        {
            result = null;
            return false;
        }

        switch (utf8Value[0])
        {
            case (byte)'S':
                if (utf8Value.SequenceEqual("String"u8))
                {
                    result = String;
                    return true;
                }

                if (utf8Value.SequenceEqual("Subscription"u8))
                {
                    result = Subscription;
                    return true;
                }

                break;

            case (byte)'I':
                if (utf8Value.SequenceEqual("Int"u8))
                {
                    result = Int;
                    return true;
                }

                if (utf8Value.SequenceEqual("ID"u8))
                {
                    result = ID;
                    return true;
                }

                break;

            case (byte)'F':
                if (utf8Value.SequenceEqual("Float"u8))
                {
                    result = Float;
                    return true;
                }

                break;

            case (byte)'B':
                if (utf8Value.SequenceEqual("Boolean"u8))
                {
                    result = Boolean;
                    return true;
                }

                break;

            case (byte)'Q':
                if (utf8Value.SequenceEqual("Query"u8))
                {
                    result = Query;
                    return true;
                }

                break;

            case (byte)'M':
                if (utf8Value.SequenceEqual("Mutation"u8))
                {
                    result = Mutation;
                    return true;
                }

                break;

            case (byte)'_':
                if (utf8Value.Length > 1 && utf8Value[1] == (byte)'_')
                {
                    if (utf8Value.SequenceEqual("__typename"u8))
                    {
                        result = __typename;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__schema"u8))
                    {
                        result = __schema;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__type"u8))
                    {
                        result = __type;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__Type"u8))
                    {
                        result = __Type;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__Field"u8))
                    {
                        result = __Field;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__InputValue"u8))
                    {
                        result = __InputValue;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__EnumValue"u8))
                    {
                        result = __EnumValue;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__Directive"u8))
                    {
                        result = __Directive;
                        return true;
                    }

                    if (utf8Value.SequenceEqual("__Schema"u8))
                    {
                        result = __Schema;
                        return true;
                    }
                }

                break;

            case (byte)'i':
                if (utf8Value.SequenceEqual("id"u8))
                {
                    result = Id;
                    return true;
                }

                if (utf8Value.SequenceEqual("input"u8))
                {
                    result = Input;
                    return true;
                }

                break;

            case (byte)'n':
                if (utf8Value.SequenceEqual("name"u8))
                {
                    result = Name;
                    return true;
                }

                if (utf8Value.SequenceEqual("node"u8))
                {
                    result = Node;
                    return true;
                }

                if (utf8Value.SequenceEqual("nodes"u8))
                {
                    result = Nodes;
                    return true;
                }

                break;

            case (byte)'d':
                if (utf8Value.SequenceEqual("description"u8))
                {
                    result = Description;
                    return true;
                }

                break;

            case (byte)'t':
                if (utf8Value.SequenceEqual("type"u8))
                {
                    result = Type;
                    return true;
                }

                break;

            case (byte)'k':
                if (utf8Value.SequenceEqual("kind"u8))
                {
                    result = Kind;
                    return true;
                }

                if (utf8Value.SequenceEqual("key"u8))
                {
                    result = Key;
                    return true;
                }

                break;

            case (byte)'f':
                if (utf8Value.SequenceEqual("fields"u8))
                {
                    result = Fields;
                    return true;
                }

                if (utf8Value.SequenceEqual("first"u8))
                {
                    result = First;
                    return true;
                }

                break;

            case (byte)'a':
                if (utf8Value.SequenceEqual("args"u8))
                {
                    result = Args;
                    return true;
                }

                if (utf8Value.SequenceEqual("after"u8))
                {
                    result = After;
                    return true;
                }

                break;

            case (byte)'e':
                if (utf8Value.SequenceEqual("edges"u8))
                {
                    result = Edges;
                    return true;
                }

                break;

            case (byte)'c':
                if (utf8Value.SequenceEqual("cursor"u8))
                {
                    result = Cursor;
                    return true;
                }

                break;

            case (byte)'l':
                if (utf8Value.SequenceEqual("last"u8))
                {
                    result = Last;
                    return true;
                }

                break;

            case (byte)'b':
                if (utf8Value.SequenceEqual("before"u8))
                {
                    result = Before;
                    return true;
                }

                break;

            case (byte)'v':
                if (utf8Value.SequenceEqual("value"u8))
                {
                    result = Value;
                    return true;
                }

                break;

            case (byte)'w':
                if (utf8Value.SequenceEqual("where"u8))
                {
                    result = Where;
                    return true;
                }

                break;

            case (byte)'o':
                if (utf8Value.SequenceEqual("order"u8))
                {
                    result = Order;
                    return true;
                }

                break;
        }

        result = null;
        return false;
    }
}
