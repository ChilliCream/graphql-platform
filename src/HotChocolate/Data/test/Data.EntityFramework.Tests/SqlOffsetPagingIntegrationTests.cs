using HotChocolate.Execution;

namespace HotChocolate.Data;

public class SqlOffsetPagingIntegrationTests : SqlLiteOffsetTestBase
{
    public TestData[] Data =>
    [
        new TestData(Guid.NewGuid(), "A"),
        new TestData(Guid.NewGuid(), "B"),
        new TestData(Guid.NewGuid(), "C"),
        new TestData(Guid.NewGuid(), "D"),
    ];

    [Fact]
    public async Task Simple_StringList_Default_Items()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task No_Boundaries_Set()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Default_Items()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Skip_2()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root(take: 2) {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Skip_2()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root(take: 2) {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Skip_2_After()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root(take: 2) {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Skip_2_After()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root(take: 2) {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Global_DefaultItem_2()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Global_DefaultItem_2()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    items {
                        foo
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task TotalCount_Should_Be_Correct()
    {
        // arrange
        var executor = CreateSchema(Data);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                root {
                    totalCount
                }
            }");

        // assert
        result.MatchSnapshot();
    }

    public class TestData
    {
        public TestData(Guid id, string foo)
        {
            Id = id;
            Foo = foo;
        }

        public Guid Id { get; set; }

        public string Foo { get; set; }
    }
}
