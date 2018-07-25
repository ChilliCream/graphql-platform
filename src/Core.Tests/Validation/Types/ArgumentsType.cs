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

            // intArgField(intArg: Int): Int
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
            descriptor.Field("multiplbooleanListArgFieldeReqs")
                .Argument("booleanListArg",
                    t => t.Type<NonNullType<ListType<BooleanType>>>())
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
        }
    }
}
