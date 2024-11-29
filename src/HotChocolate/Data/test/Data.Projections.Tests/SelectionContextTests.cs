using HotChocolate.Data.Projections.Context;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class SelectionContextTests
{
    [Fact]
    public async Task GetFields_Should_ReturnAllTheSelectedFields()
    {
        // arrange
        var list = new List<string>();

        var executor = await new ServiceCollection()
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
                    foreach (var subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(@"
            {
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_ReturnAlias()
    {
        // arrange
        var list = new List<string>();

        var executor = await new ServiceCollection()
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
                    foreach (var subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(@"
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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_WorkWithAbstractTypes()
    {
        // arrange
        var list = new List<string>();

        var executor = await new ServiceCollection()
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
                        var possibleTypes =
                            context.Schema.GetPossibleTypes(field.Type.NamedType());
                        foreach (var type in possibleTypes)
                        {
                            foreach (var subField in field.GetFields(type))
                            {
                                VisitFields(subField);
                            }
                        }
                    }
                    else
                    {
                        foreach (var subField in field.GetFields())
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
        var result = await executor.ExecuteAsync(@"
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

        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot();
    }

    [Fact]
    public async Task GetFields_Should_AssertWhenNoTypeIsSpecified()
    {
        // arrange
        Exception? ex = null;

        var executor = await new ServiceCollection()
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
                        foreach (var subField in field.GetFields())
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
        var result = await executor.ExecuteAsync(@"
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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
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

        var executor = await new ServiceCollection()
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

                    foreach (var subField in field.GetFields())
                    {
                        VisitFields(subField);
                    }

                    visitedFields.Pop();
                }

                return ValueTask.CompletedTask;
            })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(@"
            {
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        string.Join("\n", list).MatchSnapshot(selectedField);
    }
}
