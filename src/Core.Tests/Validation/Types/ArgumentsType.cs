using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class ArgumentsType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Arguments");

            descriptor.Field("multipleReqs")
                .Type<NonNullType<IntType>>()
                .Argument("x", a => a.Type<NonNullType<IntType>>())
                .Argument("y", a => a.Type<NonNullType<IntType>>())
                .Resolver(() => null);

            descriptor.Field("booleanArgField")
                .Type<BooleanType>()
                .Argument("booleanArg", a => a.Type<BooleanType>())
                .Resolver(() => null);

            descriptor.Field("floatArgField")
                .Type<FloatType>()
                .Argument("floatArg", a => a.Type<FloatType>())
                .Resolver(() => null);

            descriptor.Field("intArgField")
                .Type<IntType>()
                .Argument("intArg", a => a.Type<IntType>())
                .Resolver(() => null);

            descriptor.Field("nonNullBooleanArgField")
                .Type<NonNullType<BooleanType>>()
                .Argument("nonNullBooleanArg",
                    a => a.Type<NonNullType<BooleanType>>())
                .Resolver(() => null);

            descriptor.Field("booleanListArgField")
                .Type<ListType<BooleanType>>()
                .Argument("booleanListArg",
                    a => a.Type<NonNullType<ListType<BooleanType>>>())
                .Resolver(() => null);

            descriptor.Field("optionalNonNullBooleanArgField")
                .Type<NonNullType<BooleanType>>()
                .Argument("optionalBooleanArg",
                    a => a.Type<NonNullType<BooleanType>>())
                .Resolver(() => null);
        }
    }
}
