using System;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Processing;

public class SelectionIncludeConditionTests
{
    [Fact]
    public void Skip_True_Is_Visible_False()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        var visibility = new SelectionIncludeCondition(skip: new BooleanValueNode(true));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.False(visible);
    }

    [Fact]
    public void Skip_Var_True_Is_Visible_False()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
        var visibility = new SelectionIncludeCondition(skip: new VariableNode("b"));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.False(visible);
    }

    [Fact]
    public void Skip_False_Is_Visible_True()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        var visibility = new SelectionIncludeCondition(skip: new BooleanValueNode(false));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void Skip_Var_False_Is_Visible_True()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
        var visibility = new SelectionIncludeCondition(skip: new VariableNode("b"));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void Include_True_Is_Visible_True()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        var visibility = new SelectionIncludeCondition(include: new BooleanValueNode(true));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void Include_Var_True_Is_Visible_True()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
        var visibility = new SelectionIncludeCondition(include: new VariableNode("b"));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void Include_False_Is_Visible_False()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        var visibility = new SelectionIncludeCondition(include: new BooleanValueNode(false));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.False(visible);
    }

    [Fact]
    public void Include_Var_False_Is_Visible_False()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();
        variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
        var visibility = new SelectionIncludeCondition(include: new VariableNode("b"));

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.False(visible);
    }

    [Fact]
    public void Parent_Visible_True()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();

        var parent = new SelectionIncludeCondition(
            include: new BooleanValueNode(true));

        var visibility = new SelectionIncludeCondition(
            include: new BooleanValueNode(true),
            parent: parent);

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void Parent_Visible_False()
    {
        // arrange
        var variableValues = new Mock<IVariableValueCollection>();

        var parent = new SelectionIncludeCondition(
            include: new BooleanValueNode(false));

        var visibility = new SelectionIncludeCondition(
            include: new BooleanValueNode(true),
            parent: parent);

        // act
        var visible = visibility.IsTrue(variableValues.Object);

        // assert
        Assert.False(visible);
    }

    [Fact]
    public void Include_Is_String_GraphQLException()
    {
        // arrange
        // act
        void Action()
            => new SelectionIncludeCondition(
                include: new StringValueNode("abc"));

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Fact]
    public void Skip_Is_String_GraphQLException()
    {
        // arrange
        // act
        void Action()
            => new SelectionIncludeCondition(
                skip: new StringValueNode("abc"));

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Fact]
    public void Equals_Include_True_vs_True()
    {
        // arrange
        var a = new SelectionIncludeCondition(
            include: new BooleanValueNode(true));

        var b = new SelectionIncludeCondition(
            include: new BooleanValueNode(true));

        // act
        var equals = a.Equals(b);

        // assert
        Assert.True(equals);
    }

    [Fact]
    public void Equals_Include_True_vs_False()
    {
        // arrange
        var a = new SelectionIncludeCondition(
            include: new BooleanValueNode(true));

        var b = new SelectionIncludeCondition(
            include: new BooleanValueNode(false));

        // act
        var equals = a.Equals(b);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void Equals_Include_True_vs_Variable()
    {
        // arrange
        var a = new SelectionIncludeCondition(
            include: new BooleanValueNode(true));

        var b = new SelectionIncludeCondition(
            include: new VariableNode("b"));

        // act
        var equals = a.Equals(b);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void Equals_Include_Variable_A_vs_Variable_B()
    {
        // arrange
        var a = new SelectionIncludeCondition(
            include: new VariableNode("a"));

        var b = new SelectionIncludeCondition(
            include: new VariableNode("b"));

        // act
        var equals = a.Equals(b);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void Equals_Include_Variable_A_vs_Variable_A()
    {
        // arrange
        var a = new SelectionIncludeCondition(
            include: new VariableNode("a"));

        var b = new SelectionIncludeCondition(
            include: new VariableNode("a"));

        // act
        var equals = a.Equals(b);

        // assert
        Assert.True(equals);
    }

    [Fact]
    public async Task Skip_True_Include_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                person @skip(if: true) @include(if: true) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", true)
                        .SetVariableValue("include", true)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Variables_Skip_True_Include_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query($skip: Boolean! $include: Boolean!) {
                                person @skip(if: $skip) @include(if: $include) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", true)
                        .SetVariableValue("include", true)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Skip_True_Include_False_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                person @skip(if: true) @include(if: false) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", true)
                        .SetVariableValue("include", true)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Variables_Skip_True_Include_False_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query($skip: Boolean! $include: Boolean!) {
                                person @skip(if: $skip) @include(if: $include) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", true)
                        .SetVariableValue("include", false)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Skip_False_Include_False_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                person @skip(if: false) @include(if: false) {
                                    name
                                }
                            }
                            """)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Variables_Skip_False_Include_False_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query($skip: Boolean! $include: Boolean!) {
                                person @skip(if: $skip) @include(if: $include) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", false)
                        .SetVariableValue("include", false)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Skip_False_Include_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                person @skip(if: false) @include(if: true) {
                                    name
                                }
                            }
                            """)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "person": {
                  "name": "hello"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Variables_Skip_False_Include_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query($skip: Boolean! $include: Boolean!) {
                                person @skip(if: $skip) @include(if: $include) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", false)
                        .SetVariableValue("include", true)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "person": {
                  "name": "hello"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Skip_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                person @skip(if: true) {
                                    name
                                }
                            }
                            """)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Variables_Skip_True_Should_Be_Empty_Result()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query($skip: Boolean!) {
                                person @skip(if: $skip){
                                    name
                                }
                            }
                            """)
                        .SetVariableValue("skip", true)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    [Fact]
    public async Task Nested_Skips()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            """
                            query {
                                persons @skip(if: true) {
                                    nodes @include(if: true) @skip(if: false) {
                                        name
                                    }
                                }
                            }
                            """)
                        .Create());

        result.MatchInlineSnapshot(
            """
            {
                "data": {}
            }
            """);
    }

    public sealed class Query
    {
        public Person Person() => new Person();

        [UsePaging]
        public Person[] Persons() => new[] { new Person() };
    }

    public sealed class Person
    {
        public string Name { get; } = "hello";
    }
}
