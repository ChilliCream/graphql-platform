using System;
using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Json.Tests;

public class FromJsonDescriptorTests
{
    [Fact]
    public async Task MapField()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddObjectType(d =>
            {
                d.Name("Foo");
                d.Field("bar").Type<StringType>().FromJson();
            })
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { bar } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_With_Name()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddObjectType(d =>
            {
                d.Name("Foo");
                d.Field("baz").Type<StringType>().FromJson("bar");
            })
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { baz } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_Explicitly()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddObjectType(d =>
            {
                d.Name("Foo");
                d.Field("baz").Type<StringType>().FromJson(t => t.GetProperty("bar").GetString());
            })
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { baz } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void FromJson_1_Descriptor_Is_Null()
    {
        void Fail() => JsonObjectTypeExtensions.FromJson(null!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void FromJson_2_Descriptor_Is_Null()
    {
        void Fail() => JsonObjectTypeExtensions.FromJson(null!, element => "");
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void FromJson_2_Resolver_Is_Null()
    {
        var mock = new Mock<IObjectFieldDescriptor>();
        void Fail() => JsonObjectTypeExtensions.FromJson(
            mock.Object,
            default(Func<JsonElement, string>)!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddJsonSupport_Builder_Is_Null()
    {
        void Fail() => JsonRequestExecutorBuilderExtensions.AddJsonSupport(null!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    public class Query
    {
        [GraphQLType("Foo")]
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": ""abc"" }").RootElement;
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("bar").Type<StringType>().FromJson();
        }
    }

    public class FooTypeWithExplicitMapping : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("baz").Type<StringType>().FromJson("bar");
        }
    }
}
