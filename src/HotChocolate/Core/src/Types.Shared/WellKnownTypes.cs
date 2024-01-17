using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities.Introspection;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class WellKnownTypes
{
    public const string __Directive = "__Directive";
    public const string __DirectiveLocation = "__DirectiveLocation";
    public const string __EnumValue = "__EnumValue";
    public const string __Field = "__Field";
    public const string __InputValue = "__InputValue";
    public const string __Schema = "__Schema";
    public const string __Type = "__Type";
    public const string __TypeKind = "__TypeKind";
    public const string String = "String";
    public const string Boolean = "Boolean";
    public const string Float = "Float";
    public const string ID = "ID";
    public const string Int = "Int";
}
