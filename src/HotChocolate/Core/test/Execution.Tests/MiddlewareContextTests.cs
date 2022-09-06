using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

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
            .AddResolver("Query", "foo", ctx =>
                ctx.Variables.GetVariable<string>("abc"))
            .Create();

        var request = QueryRequestBuilder.New()
            .SetQuery("query abc($abc: String){ foo(bar: $abc) }")
            .SetVariableValue("abc", "def")
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(request);

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
            .AddResolver("Query", "foo", ctx =>
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
            .Use(_ => context =>
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
        Snapshot.FullName();
        var services = new DictionaryServiceProvider(typeof(string), "hello");

        // assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name(OperationTypeNames.Query);

                d.Field("foo")
                    .Resolve(ctx => ctx.Service<string>())
                    .Use(next => async context =>
                    {
                        context.Services = services;
                        await next(context);
                    });
            })
            .ExecuteRequestAsync("{ foo }")

            // assert
            .MatchSnapshotAsync();
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