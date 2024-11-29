using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types.Pagination;

public class UsePagingAttributeTests
{
    [Fact]
    public async Task UsePagingAttribute_Infer_Types()
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
    public async Task UsePagingAttribute_Execute_Query()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider()
            .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UsePagingAttribute_Infer_Types_On_Interface()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddType<IHasFoos>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Services
            .BuildServiceProvider()
            .GetSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UsePagingAttribute_On_Extension_Infer_Types()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<QueryExtension>()
            .Services
            .BuildServiceProvider()
            .GetSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UsePagingAttribute_On_Extension_Execute_Query()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<QueryExtension>()
            .Services
            .BuildServiceProvider()
            .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Attributes_Are_Applied_Once()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query1>()
            .AddType<Query1Extensions>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Attributes_Are_Applied_Once_Execute_Query()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query1>()
            .AddType<Query1Extensions>()
            .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UnknownNodeType()
    {
        try
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddType<NoNodeType>()
                .BuildSchemaAsync();
        }
        catch (SchemaException ex)
        {
            new
            {
                ex.Errors[0].Message,
                ex.Errors[0].Code,
            }.MatchSnapshot();
        }
    }

    [Fact]
    public void UsePagingAttribute_Can_Use_Defaults()
    {
        var attr = new UsePagingAttribute();

        Assert.True(attr.AllowBackwardPagination);
        Assert.True(attr.InferConnectionNameFromField);
        Assert.False(attr.RequirePagingBoundaries);
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
        }
    }

    public class Query
    {
        [UsePaging]
        public IQueryable<Foo> Foos ()
        {
            return new List<Foo>
            {
                new(bar: "first"),
                new(bar: "second"),
            }.AsQueryable();
        }
    }

    public class Query1
    {
        public IQueryable<Foo> Foos ()
        {
            return new List<Foo>
            {
                new(bar: "first"),
                new(bar: "second"),
            }.AsQueryable();
        }
    }

    [Node]
    [ExtendObjectType(typeof(Query1))]
    public class Query1Extensions
    {
        [UsePaging]
        [BindMember(nameof(Query1.Foos))]
        public IQueryable<Foo> Foos ()
        {
            return new List<Foo>
            {
                new(bar: "first"),
                new(bar: "second"),
            }.AsQueryable();
        }

        [NodeResolver]
        public static Query1 GetQuery()
            => new();
    }

    [ExtendObjectType("Query")]
    public class QueryExtension : Query
    {
    }

    public class Foo(string bar)
    {
        public string Bar { get; set; } = bar;
    }

    public interface IHasFoos
    {
        [UsePaging]
        IQueryable<Foo> Foos { get; }
    }

    [ExtendObjectType("Query")]
    public class NoNodeType
    {
        [UsePaging]
        public int GetSomething() => 1;
    }
}
