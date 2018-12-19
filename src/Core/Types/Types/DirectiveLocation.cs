using System;

namespace HotChocolate.Types
{
    [Flags]
    public enum DirectiveLocation
    {
        Query = 0x1,
        Mutation = 0x2,
        Subscription = 0x4,
        Field = 0x8,
        FragmentDefinition = 0x10,
        FragmentSpread = 0x20,
        InlineFragment = 0x40,
        Schema = 0x80,
        Scalar = 0x100,
        Object = 0x200,
        FieldDefinition = 0x400,
        ArgumentDefinition = 0x800,
        Interface = 0x1000,
        Union = 0x2000,
        Enum = 0x4000,
        EnumValue = 0x8000,
        InputObject = 0x10000,
        InputFieldDefinition = 0x20000
    }
}
