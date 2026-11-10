using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;

namespace HotChocolate.Types.Pagination;

public class Issue4790Tests
{
    [Fact]
    public async Task UsePaging_With_Same_Field_Name_On_Different_Parents_Uses_Correct_Connection_Type()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Issue4790Query>()
            .AddType<Issue4790FooType>()
            .AddType<Issue4790BarType>()
            .Services
            .BuildServiceProvider()
            .GetSchemaAsync();

        // act
        var fooType = schema.Types.GetType<ObjectType>("Foo");
        var barType = schema.Types.GetType<ObjectType>("Bar");

        var fooBazzesConnection = fooType.Fields["bazzes"].Type.NamedType().Name;
        var barBazzesConnection = barType.Fields["bazzes"].Type.NamedType().Name;
        // assert
        Assert.Equal("BazzesConnection", fooBazzesConnection);
        Assert.NotEqual(fooBazzesConnection, barBazzesConnection);
        Assert.Equal("Issue4790BazzSummaryConnection", barBazzesConnection);
    }

    public sealed class Issue4790Query
    {
        public Issue4790Foo Foo() => new();

        public Issue4790Bar Bar() => new();
    }

    public sealed class Issue4790Foo;

    public sealed class Issue4790Bar
    {
        public string? Qux { get; set; }
    }

    public sealed class Issue4790Bazz
    {
        public string? Field1 { get; set; }

        public string? ExpensiveField2 { get; set; }
    }

    public sealed class Issue4790BazzSummary
    {
        public string? Field1 { get; set; }
    }

    public sealed class Issue4790FooType : ObjectType<Issue4790Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Issue4790Foo> descriptor)
        {
            descriptor.Name("Foo");

            descriptor
                .Field("bazzes")
                .UsePaging<ObjectType<Issue4790Bazz>>()
                .Resolve(_ =>
                    new[]
                    {
                        new Issue4790Bazz(),
                        new Issue4790Bazz()
                    }.AsQueryable());
        }
    }

    public sealed class Issue4790BarType : ObjectType<Issue4790Bar>
    {
        protected override void Configure(IObjectTypeDescriptor<Issue4790Bar> descriptor)
        {
            descriptor.Name("Bar");

            descriptor
                .Field("bazzes")
                .UsePaging<ObjectType<Issue4790BazzSummary>>()
                .Resolve(_ =>
                    new[]
                    {
                        new Issue4790BazzSummary(),
                        new Issue4790BazzSummary()
                    }.AsQueryable());

            descriptor
                .Field("bazzSummaries")
                .UsePaging<ObjectType<Issue4790BazzSummary>>()
                .Resolve(_ =>
                    new[]
                    {
                        new Issue4790BazzSummary(),
                        new Issue4790BazzSummary()
                    }.AsQueryable());
        }
    }
}
