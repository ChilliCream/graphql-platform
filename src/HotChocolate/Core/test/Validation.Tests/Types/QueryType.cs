using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field("arguments")
            .Type<ArgumentsType>()
            .Resolve(() => null!);

        descriptor.Field("invalidArg")
            .Type<StringType>()
            .Argument("arg", a => a.Type<InvalidScalar>())
            .Resolve(() => null!);

        descriptor.Field("anyArg")
            .Type<StringType>()
            .Argument("arg", a => a.Type<AnyType>())
            .Resolve(() => null!);

        descriptor.Field("field")
            .Type<StringType>()
            .Argument("a", a => a.Type<StringType>())
            .Argument("b", a => a.Type<StringType>())
            .Argument("c", a => a.Type<StringType>())
            .Argument("d", a => a.Type<StringType>())
            .Type<QueryType>()
            .Resolve(() => null!);

        descriptor.Field(t => t.GetCatOrDog())
            .Type<CatOrDogType>();

        descriptor.Field(t => t.GetDogOrHuman())
            .Type<DogOrHumanType>();

        descriptor.Field("nonNull")
            .Argument("a", a => a.Type<NonNullType<StringType>>().DefaultValue("abc"))
            .Resolve("foo");

        descriptor.Field("listOfScalars")
            .Type<ListType<StringType>>()
            .Resolve<string[]>(_ => []);

        descriptor.Field("listOfListOfScalars")
            .Type<ListType<ListType<StringType>>>()
            .Resolve<string[][]>(_ => []);
    }
}
