using HotChocolate.Types;

namespace HotChocolate.Validation
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

            // intArgField(intArg: Int): Int!
            descriptor.Field("intArgField")
                .Argument("intArg", t => t.Type<IntType>())
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

            // intArgField(intArg: String): String!
            descriptor.Field("stringArgField")
                .Argument("stringArg", t => t.Type<StringType>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);

            // intArgField(intArg: DogCommand): String!
            descriptor.Field("enumArgField")
                .Argument("enumArg", t => t.Type<EnumType<DogCommand>>())
                .Type<NonNullType<StringType>>()
                .Resolver(() => null);
        }
    }
}
