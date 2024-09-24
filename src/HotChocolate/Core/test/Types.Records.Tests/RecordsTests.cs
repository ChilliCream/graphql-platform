using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types.Relay;
using static HotChocolate.Tests.TestHelper;
using HotChocolate.Tests;
using HotChocolate.Execution;

namespace HotChocolate.Types;

public class RecordsTests
{
    [Fact]
    public async Task Records_Clone_Member_Is_Removed()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider()
            .GetSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Records_Default_Value_Is_Taken_From_Ctor()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .Services
            .BuildServiceProvider()
            .GetSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Records_Input_Ignored_Default_Value_Is_Respected()
    {
        await ExpectValid(
            "{ foo(input: { bar: 42 }) { bar baz qux quux } }",
            b => b.AddQueryType(type =>
            {
                type.Field("foo")
                    .Type(new ObjectType<Foo>())
                    .Argument(
                        "input",
                        argument =>
                        {
                            argument.Type(new InputObjectType<Foo>(type =>
                                type.BindFieldsExplicitly()
                                    .Field(x => x.Bar)));
                        })
                    .Resolve(context =>
                        context.ArgumentValue<Foo>("input"));
            }))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Relay_Id_Middleware_Is_Correctly_Applied()
    {
        await ExpectValid
        (
            @"{ person { id name } }",
            b => b.AddQueryType<Query>().AddGlobalObjectIdentification(false)
        )
        .MatchSnapshotAsync();
    }

    public class Query
    {
        public Person GetPerson() => new Person(1, "Michael");
    }

    public record Person([property: ID] int Id, string Name);

    public class Query2
    {
        public DefaultValueTest GetPerson(DefaultValueTest? defaultValueTest) => new(1, "Test");
    }

    public record DefaultValueTest([property: ID] int Id, string Name = "ShouldBeDefaultValue");

    public record Foo(int Bar, int? Baz = 84, DateTimeOffset Qux = default, int[]? Quux = default) {
        public int[] Quux { get; init; } = Quux ?? [1];
    }
}
