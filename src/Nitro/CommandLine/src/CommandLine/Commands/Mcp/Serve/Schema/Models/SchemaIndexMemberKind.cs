using System.ComponentModel;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal enum SchemaIndexMemberKind
{
    [Description("Object type definitions")]
    Type,

    [Description("Fields on object or interface types")]
    Field,

    [Description("Arguments on fields or directives")]
    Argument,

    [Description("Enum type definitions")]
    Enum,

    [Description("Individual enum values")]
    EnumValue,

    [Description("Input object type definitions")]
    InputType,

    [Description("Fields on input object types")]
    InputField,

    [Description("Interface type definitions")]
    Interface,

    [Description("Union type definitions")]
    Union,

    [Description("Scalar type definitions")]
    Scalar,

    [Description("Directive definitions")]
    Directive
}
