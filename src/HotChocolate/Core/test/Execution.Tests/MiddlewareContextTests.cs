using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

public class MiddlewareContextTests
{
    [Fact]
    public async Task AccessVariables()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                "type Query { foo(bar: String) : String }")
            .AddResolver(
                "Query",
                "foo",
                ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
            .Create();

        var request = QueryRequestBuilder.New()
            .SetQuery("query abc($abc: String){ foo(bar: $abc) }")
            .SetVariableValue("abc", "def")
            .Create();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AccessVariables_Fails_When_Variable_Not_Exists()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                "type Query { foo(bar: String) : String }")
            .AddResolver(
                "Query",
                "foo",
                ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
            .Create();

        var request = QueryRequestBuilder.New()
            .SetQuery("query abc($def: String){ foo(bar: $def) }")
            .SetVariableValue("def", "ghi")
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task CollectFields()
    {
        // arrange
        var list = new List<ISelection>();

        var schema = SchemaBuilder.New()
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
            .Use(
                _ => context =>
                {
                    if (context.Selection.Type.NamedType() is ObjectType type)
                    {
                        foreach (var selection in context.GetSelections(type))
                        {
                            CollectSelections(context, selection, list);
                        }
                    }
                    return default;
                })
            .Create();

        // act
        await schema.MakeExecutable().ExecuteAsync(
            @"{
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        list.Select(t => t.SyntaxNode.Name.Value).ToList().MatchSnapshot();
    }

    [Fact]
    public async Task CustomServiceProvider()
    {
        // arrange
        var services = new DictionaryServiceProvider(typeof(string), "hello");

        // assert
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    d =>
                    {
                        d.Name(OperationTypeNames.Query);

                        d.Field("foo")
                            .Resolve(ctx => ctx.Service<string>())
                            .Use(
                                next => async context =>
                                {
                                    context.Services = services;
                                    await next(context);
                                });
                    })
                .ExecuteRequestAsync("{ foo }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ReplaceArguments_Delegate()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                var original =
                                    context.ReplaceArguments(
                                        current =>
                                        {
                                            var arguments = new Dictionary<string, ArgumentValue>();

                                            foreach (var argumentValue in current.Values)
                                            {
                                                if (argumentValue.Type.RuntimeType ==
                                                    typeof(string) &&
                                                    argumentValue
                                                        .ValueLiteral is StringValueNode sv)
                                                {
                                                    sv = sv.WithValue(sv.Value.Trim());
                                                    var trimmedArgument = new ArgumentValue(
                                                        argumentValue,
                                                        ValueKind.String,
                                                        false,
                                                        false,
                                                        null,
                                                        sv);
                                                    arguments.Add(
                                                        argumentValue.Name,
                                                        trimmedArgument);
                                                }
                                                else
                                                {
                                                    arguments.Add(
                                                        argumentValue.Name,
                                                        argumentValue);
                                                }
                                            }

                                            return arguments;
                                        });

                                await next(context);

                                context.ReplaceArguments(original);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc   \") }");

        result.MatchSnapshot();
    }

     [Fact]
    public async Task ReplaceArguments_Delegate_ReplaceWithNull_ShouldFail()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                var original = context.ReplaceArguments(_ => null);

                                await next(context);

                                context.ReplaceArguments(original);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc   \") }");

        result.MatchSnapshot();
    }

    private static void CollectSelections(
        IResolverContext context,
        ISelection selection,
        ICollection<ISelection> collected)
    {
        if (selection.Type.IsLeafType())
        {
            collected.Add(selection);
        }

        if (selection.Type.NamedType() is ObjectType objectType)
        {
            foreach (var child in context.GetSelections(objectType, selection))
            {
                CollectSelections(context, child, collected);
            }
        }
    }
}
