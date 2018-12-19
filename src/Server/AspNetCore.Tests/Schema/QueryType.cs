using HotChocolate.Types;

namespace HotChocolate.AspNetCore
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetBasic())
                .Type<ObjectType<Foo>>();
            descriptor.Field(t => t.GetWithScalarArgument(default))
                .Type<ObjectType<Foo>>()
                .Argument("a", a => a.Type<NonNullType<StringType>>());
            descriptor.Field(t => t.GetWithObjectArgument(default))
                .Type<ObjectType<Foo>>()
                .Argument("b", a =>
                    a.Type<NonNullType<FooInputType>>()
                        .DefaultValue(new Foo { A = "hello world", C = 0 }));
            descriptor.Field(t => t.GetWithEnum(default))
                .Type<NonNullType<BooleanType>>()
                .Argument("test", a => a.Type<EnumType<TestEnum>>());
            descriptor.Field("customProperty")
                .Resolver(ctx => ctx.CustomProperty<string>("foo"));
        }
    }
}
