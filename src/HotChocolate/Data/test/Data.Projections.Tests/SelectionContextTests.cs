using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Projections.Context;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class SelectionContextTests
{
    [Fact]
    public async Task GetFields_Should_ReturnAllTheSelectedFields()
    {
        // arrange
        var list = new List<string>();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo: Foo
                    }

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
            .UseField(_ => context =>
            {
                if (list.Count > 0)
                {
                    return ValueTask.CompletedTask;
                }

                Stack<string> visitedFields = new();
                VisitFields(context.GetSelectedField());

                void VisitFields(ISelectedField field)
                {
                    visitedFields.Push(field.Field.Name);
                    list.Add(string.Join(".", visitedFields.Reverse()));
                    foreach (ISelectedField subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_ReturnAlias()
    {
        // arrange
        var list = new List<string>();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo: Foo
                    }

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
            .UseField(_ => context =>
            {
                if (list.Count > 0)
                {
                    return ValueTask.CompletedTask;
                }

                Stack<string> visitedFields = new();
                VisitFields(context.GetSelectedField());

                void VisitFields(ISelectedField field)
                {
                    visitedFields.Push(field.Selection.ResponseName);
                    list.Add(string.Join(".", visitedFields.Reverse()));
                    foreach (ISelectedField subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foo {
                    bar {
                        a: baz
                        b: baz
                        c: baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_WorkWithAbstractTypes()
    {
        // arrange
        var list = new List<string>();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo: Qux
                    }

                    union Qux = Foo | Bar

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
            .UseField(_ => context =>
            {
                if (list.Count > 0)
                {
                    return ValueTask.CompletedTask;
                }

                Stack<string> visitedFields = new();
                VisitFields(context.GetSelectedField());

                void VisitFields(ISelectedField field)
                {
                    visitedFields.Push(field.Field.Name);
                    list.Add(string.Join(".", visitedFields.Reverse()));

                    if (field.IsAbstractType)
                    {
                        IReadOnlyList<ObjectType> possibleTypes =
                            context.Schema.GetPossibleTypes(field.Type.NamedType());
                        foreach (ObjectType type in possibleTypes)
                        {
                            foreach (ISelectedField subField in field.GetFields(type))
                            {
                                VisitFields(subField);
                            }
                        }
                    }
                    else
                    {
                        foreach (ISelectedField subField in field.GetFields())
                        {
                            VisitFields(subField);
                        }
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foo {
                    ... on Foo {
                        bar {
                            baz
                        }
                    }
                    ... on Bar {
                        baz
                    }
                }
            }");

        // assert

        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_AssertWhenNoTypeIsSpecified()
    {
        // arrange
        Exception? ex = null;

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo: Qux
                    }

                    union Qux = Foo | Bar

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
            .UseField(_ => context =>
            {
                if (ex is not null)
                {
                    return ValueTask.CompletedTask;
                }

                try
                {
                    VisitFields(context.GetSelectedField());

                    void VisitFields(ISelectedField field)
                    {
                        foreach (ISelectedField subField in field.GetFields())
                        {
                            VisitFields(subField);
                        }
                    }
                }
                catch (Exception e)
                {
                    ex = e;
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foo {
                    ... on Foo {
                        bar {
                            baz
                        }
                    }
                    ... on Bar {
                        baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        Assert.IsType<InvalidOperationException>(ex).Message.MatchSnapshot();
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    public async Task IsSelected_Should_ReturnTrueIfTheFieldIsSelected(
        string selectedField)
    {
        // arrange
        var list = new List<string>();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo: Foo
                    }

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
            .UseField(_ => context =>
            {
                if (list.Count > 0)
                {
                    return ValueTask.CompletedTask;
                }

                Stack<string> visitedFields = new();
                VisitFields(context.GetSelectedField());

                void VisitFields(ISelectedField field)
                {
                    visitedFields.Push(field.Field.Name);

                    list.Add(
                        $"{string.Join(".", visitedFields.Reverse())}" +
                        $":{field.IsSelected(selectedField)}");

                    foreach (ISelectedField subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot(new SnapshotNameExtension(selectedField));
    }
}
