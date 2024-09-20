using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class ArgumentsType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Arguments");

        // multipleReqs(x: Int!, y: Int!): Int!
        descriptor.Field("multipleReqs")
            .Argument("x", t => t.Type<NonNullType<IntType>>())
            .Argument("y", t => t.Type<NonNullType<IntType>>())
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        descriptor.Field("multipleOpts")
            .Argument("opt1", t => t.Type<IntType>())
            .Argument("opt2", t => t.Type<IntType>())
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        descriptor.Field("multipleOptsAndReqs")
            .Argument("req1", t => t.Type<NonNullType<IntType>>())
            .Argument("req2", t => t.Type<NonNullType<IntType>>())
            .Argument("opt1", t => t.Type<IntType>())
            .Argument("opt2", t => t.Type<IntType>())
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        //  booleanArgField(booleanArg: Boolean) : Boolean
        descriptor.Field("booleanArgField")
            .Argument("booleanArg", t => t.Type<BooleanType>())
            .Type<BooleanType>()
            .Resolve(() => null!);

        // floatArgField(floatArg: Float): Float
        descriptor.Field("floatArgField")
            .Argument("floatArg", t => t.Type<FloatType>())
            .Type<FloatType>()
            .Resolve(() => null!);

        // nonNullFloatArgField(floatArg: Float): Float
        descriptor.Field("nonNullFloatArgField")
            .Argument("floatArg", t => t.Type<NonNullType<FloatType>>())
            .Type<FloatType>()
            .Resolve(() => null!);

        // intArgField(intArg: Int): Int!
        descriptor.Field("intArgField")
            .Argument("intArg", t => t.Type<IntType>())
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        // nonNullIntArgField(intArg: Int!): Int!
        descriptor.Field("nonNullIntArgField")
            .Argument("intArg", t => t.Type<NonNullType<IntType>>())
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        // nonNullBooleanArgField(nonNullBooleanArg: Boolean!): Boolean!
        descriptor.Field("nonNullBooleanArgField")
            .Argument("nonNullBooleanArg",
                t => t.Type<NonNullType<BooleanType>>())
            .Type<NonNullType<BooleanType>>()
            .Resolve(() => null!);

        // booleanListArgField(booleanListArg: [Boolean]!) : [Boolean]
        descriptor.Field("booleanListArgField")
            .Argument("booleanListArg",
                t => t.Type<NonNullType<ListType<BooleanType>>>())
            .Type<ListType<BooleanType>>()
            .Resolve(() => null!);

        // nonNullBooleanListArgField(booleanListArg: [Boolean!]!) : [Boolean]
        descriptor.Field("nonNullBooleanListArgField")
            .Argument("booleanListArg",
                t => t.Type<NonNullType<ListType<NonNullType<BooleanType>>>>())
            .Type<ListType<BooleanType>>()
            .Resolve(() => null!);

        // optionalNonNullBooleanArgField(optionalBooleanArg: Boolean! = false) : Boolean!
        descriptor.Field("optionalNonNullBooleanArgField")
            .Argument("optionalBooleanArg",
                t => t.Type<NonNullType<BooleanType>>().DefaultValue(false))
            .Argument("y", t => t.Type<NonNullType<IntType>>())
            .Type<NonNullType<BooleanType>>()
            .Resolve(() => null!);

        // optionalNonNullBooleanArgField2(optionalBooleanArg: Boolean = true) : Boolean!
        descriptor.Field("optionalNonNullBooleanArgField2")
            .Argument("optionalBooleanArg",
                t => t.Type<BooleanType>().DefaultValue(true))
            .Type<BooleanType>()
            .Resolve(() => null!);

        // booleanListArgField(booleanListArg: [Boolean]!) : [Boolean]
        descriptor.Field("nonNullBooleanListField")
            .Argument("nonNullBooleanListArg",
                t => t.Type<NonNullType<ListType<BooleanType>>>())
            .Type<ListType<BooleanType>>()
            .Resolve(() => null!);

        // intArgField(intArg: ID): ID!
        descriptor.Field("idArgField")
            .Argument("idArg", t => t.Type<IdType>())
            .Type<NonNullType<IdType>>()
            .Resolve(() => null!);

        descriptor.Field("nonNullIdArgField")
            .Argument("idArg", t => t.Type<NonNullType<IdType>>())
            .Type<NonNullType<IdType>>()
            .Resolve(() => null!);

        // stringArgField(intArg: String): String!
        descriptor.Field("stringArgField")
            .Argument("stringArg", t => t.Type<StringType>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("nonNullStringArgField")
            .Argument("stringArg", t => t.Type<NonNullType<StringType>>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("stringListArgField")
            .Argument("stringListArg",
                t => t.Type<ListType<StringType>>())
            .Type<ListType<StringType>>()
            .Resolve(() => null!);

        // enumArgField(intArg: DogCommand): String!
        descriptor.Field("enumArgField")
            .Argument("enumArg", t => t.Type<EnumType<DogCommand>>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        // "nonNullEnumArgField(intArg: DogCommand!): String!
        descriptor.Field("nonNullEnumArgField")
            .Argument("enumArg", t => t.Type<NonNullType<EnumType<DogCommand>>>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("complexArgField")
            .Argument("complexArg", t => t.Type<Complex3InputType>())
            .Type<NonNullType<StringType>>()
            .Argument("complexArg1", t => t.Type<Complex3InputType>())
            .Type<NonNullType<StringType>>()
            .Argument("complexArg2", t => t.Type<Complex3InputType>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("nonNullFieldWithDefault")
            .Argument("opt1", t => t.Type<NonNullType<IntType>>().DefaultValue(0))
            .Type<NonNullType<IntType>>()
            .Resolve(() => null!);

        descriptor.Field("nonNullFieldWithDefault")
            .Argument("nonNullIntArg", t => t.Type<NonNullType<IntType>>().DefaultValue(0))
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("nonNullField")
            .Argument("nonNullIntArg", t => t.Type<NonNullType<IntType>>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);

        descriptor.Field("stringListNonNullArgField")
            .Argument(
                "stringListNonNullArg",
                t => t.Type<NonNullType<ListType<StringType>>>())
            .Type<NonNullType<StringType>>()
            .Resolve(() => null!);
    }
}
