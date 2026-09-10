using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue6953Tests
{
    [Fact]
    public async Task UseProjection_Should_ProjectUnionMembers_When_FieldTypeIsExplicitUnion()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              unionTest {
                __typename
                ... on ChildA {
                  a
                }
                ... on ChildB {
                  b
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "unionTest": [
                  {
                    "__typename": "ChildA",
                    "a": "value-a"
                  },
                  {
                    "__typename": "ChildB",
                    "b": "value-b"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task UseProjection_Should_PreserveRuntimeTypes_When_OnlyTypenameIsSelected()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              unionTest {
                __typename
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "unionTest": [
                  {
                    "__typename": "ChildA"
                  },
                  {
                    "__typename": "ChildB"
                  }
                ]
              }
            }
            """);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<Query>()
            .AddType<ChildAType>()
            .AddType<ChildBType>()
            .AddType<UnionTestType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public class Query
    {
        [UseProjection]
        [GraphQLType(typeof(ListType<UnionTestType>))]
        public IQueryable<Base> GetUnionTest()
            => Data.AsQueryable();
    }

    public class Base
    {
        public string C { get; set; } = string.Empty;
    }

    public class ChildA : Base
    {
        public string A { get; set; } = string.Empty;
    }

    public class ChildB : Base
    {
        public string B { get; set; } = string.Empty;
    }

    public class ChildAType : ObjectType<ChildA>
    {
        protected override void Configure(IObjectTypeDescriptor<ChildA> descriptor)
        {
            descriptor.Field(x => x.A).Type<NonNullType<StringType>>();
        }
    }

    public class ChildBType : ObjectType<ChildB>
    {
        protected override void Configure(IObjectTypeDescriptor<ChildB> descriptor)
        {
            descriptor.Field(x => x.B).Type<NonNullType<StringType>>();
        }
    }

    public class UnionTestType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("UnionTestType");
            descriptor.Type<ChildAType>();
            descriptor.Type<ChildBType>();
        }
    }

    private static readonly Base[] Data =
    [
        new ChildA { C = "shared-a", A = "value-a" },
        new ChildB { C = "shared-b", B = "value-b" }
    ];
}
