using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class ArgumentsType
        : ObjectType
    {
        public ArgumentsType()
        {
        }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Arguments");

            // multipleReqs(x: Int!, y: Int!): Int!
            descriptor.Field("multipleReqs")
                .Argument("x", t => t.Type<NonNullType<IntType>>())
                .Argument("y", t => t.Type<NonNullType<IntType>>())
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            descriptor.Field("multipleOpts")
                .Argument("opt1", t => t.Type<IntType>())
                .Argument("opt2", t => t.Type<IntType>())
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            descriptor.Field("multipleOptsAndReqs")
                .Argument("req1", t => t.Type<NonNullType<IntType>>())
                .Argument("req2", t => t.Type<NonNullType<IntType>>())
                .Argument("opt1", t => t.Type<IntType>())
                .Argument("opt2", t => t.Type<IntType>())
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            //  booleanArgField(booleanArg: Boolean) : Boolean
            descriptor.Field("booleanArgField")
                .Argument("booleanArg", t => t.Type<BooleanType>())
                .Type<BooleanType>()
                .Resolver(() => null);

            // floatArgField(floatArg: Float): Float
            descriptor.Field("floatArgField")
                .Argument("floatArg", t => t.Type<FloatType>())
                .Type<FloatType>()
                .Resolver(() => null);

            // nonNullFloatArgField(floatArg: Float): Float
            descriptor.Field("nonNullFloatArgField")
                .Argument("floatArg", t => t.Type<NonNullType<FloatType>>())
                .Type<FloatType>()
                .Resolver(() => null);

            // intArgField(intArg: Int): Int!
            descriptor.Field("intArgField")
                .Argument("intArg", t => t.Type<IntType>())
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            // nonNullIntArgField(intArg: Int!): Int!
            descriptor.Field("nonNullIntArgField")
                .Argument("intArg", t => t.Type<NonNullType<IntType>>())
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            // nonNullBooleanArgField(nonNullBooleanArg: Boolean!): Boolean!
            descriptor.Field("nonNullBooleanArgField")
                .Argument("nonNullBooleanArg",
                    t => t.Type<NonNullType<BooleanType>>())
                .Type<NonNullType<BooleanType>>()
                .Resolver(() => null);

            // booleanListArgField(booleanListArg: [Boolean]!) : [Boolean]
            descriptor.Field("booleanListArgField")
                .Argument("booleanListArg",
                    t => t.Type<NonNullType<ListType<BooleanType>>>())
                .Type<ListType<BooleanType>>()
                .Resolver(() => null);

            // nonNullBooleanListArgField(booleanListArg: [Boolean!]!) : [Boolean]
            descriptor.Field("nonNullBooleanListArgField")
                .Argument("booleanListArg",
                    t => t.Type<NonNullType<ListType<NonNullType<BooleanType>>>>())
                .Type<ListType<BooleanType>>()
                .Resolver(() => null);

            // optionalNonNullBooleanArgField(optionalBooleanArg: Boolean! = false) : Boolean!
            descriptor.Field("optionalNonNullBooleanArgField")
                .Argument("optionalBooleanArg",
                    t => t.Type<NonNullType<BooleanType>>().DefaultValue(false))
                .Argument("y", t => t.Type<NonNullType<IntType>>())
                .Type<NonNullType<BooleanType>>()
                .Resolver(() => null);

            // optionalNonNullBooleanArgField2(optionalBooleanArg: Boolean = true) : Boolean!
            descriptor.Field("optionalNonNullBooleanArgField2")
                .Argument("optionalBooleanArg",
                    t => t.Type<BooleanType>().DefaultValue(true))
                .Type<BooleanType>()
                .Resolver(() => null);

            // booleanListArgField(booleanListArg: [Boolean]!) : [Boolean]
            descriptor.Field("nonNullBooleanListField")
                .Argument("nonNullBooleanListArg",
                    t => t.Type<NonNullType<ListType<BooleanType>>>())
                .Type<ListType<BooleanType>>()
                .Resolver(() => null);

            // intArgField(intArg: ID): ID!
            descriptor.Field("idArgField")
                .Argument("idArg", t => t.Type<IdType>())
                .Type<NonNullType<IdType>>()
                .Resolver(() => null);

            descriptor.Field("nonNullIdArgField")
                .Argument("idArg", t => t.Type<NonNullType<IdType>>())
                .Type<NonNullType<IdType>>()
                .Resolver(() => null);

            // stringArgField(intArg: String): String!
            descriptor.Field("stringArgField")
                .Argument("stringArg", t => t.Type<StringType>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("nonNullStringArgField")
                .Argument("stringArg", t => t.Type<NonNullType<StringType>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("stringListArgField")
                .Argument("stringListArg",
                    t => t.Type<ListType<StringType>>())
                .Type<ListType<StringType>>()
                .Resolver(() => null);

            // enumArgField(intArg: DogCommand): String!
            descriptor.Field("enumArgField")
                .Argument("enumArg", t => t.Type<EnumType<DogCommand>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            // "nonNullenumArgField(intArg: DogCommand!): String!
            descriptor.Field("nonNullEnumArgField")
                .Argument("enumArg", t => t.Type<NonNullType<EnumType<DogCommand>>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("complexArgField")
                .Argument("complexArg", t => t.Type<Complex3InputType>())
                .Type<NonNullType<StringType>>()
                .Argument("complexArg1", t => t.Type<Complex3InputType>())
                .Type<NonNullType<StringType>>()
                .Argument("complexArg2", t => t.Type<Complex3InputType>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("nonNullFieldWithDefault")
                .Argument("opt1", t => t.Type<NonNullType<IntType>>().DefaultValue(0))
                .Type<NonNullType<IntType>>()
                .Resolver(() => null);

            descriptor.Field("nonNullFieldWithDefault")
                .Argument("nonNullIntArg", t => t.Type<NonNullType<IntType>>().DefaultValue(0))
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("nonNullField")
                .Argument("nonNullIntArg", t => t.Type<NonNullType<IntType>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            descriptor.Field("stringListNonNullArgField")
                .Argument(
                    "stringListNonNullArg",
                    t => t.Type<NonNullType<ListType<StringType>>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);
        }
    }
}
