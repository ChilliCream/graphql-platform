using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;
using HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;
using HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types.Pipeline;

public class ApplyLocalRenamingMiddlewareTests
{
    [Fact]
    public async Task Apply_Local_Rename()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "abc",
            Utf8GraphQLParser.Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo {
                    abc: String @rename(to: ""def"")
                    def: Int
                    ghi: Int @_hc_bind(to: ""bar"" as: ""baz"")
                }"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Local_Rename_Interface_Name()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "abc",
            Utf8GraphQLParser.Parse(@"
                type Foo implements IFoo {
                    abc: String
                }

                interface IFoo {
                    abc: String
                }

                extend interface IFoo @rename(to: ""IDef"")"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Local_Rename_Object_And_Refactor_Usages()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "abc",
            Utf8GraphQLParser.Parse(@"
                type Foo @rename(to: ""Bar"") {
                    abc: String
                }

                type Baz {
                    foo: Foo
                }

                union FooOrBaz = Foo | Baz"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Local_Rename_Scalar_And_Update_Usages()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "abc",
            Utf8GraphQLParser.Parse(@"
                extend scalar String @rename(to: ""SpecialString"")

                type Foo {
                    abc(input: FooInput): String
                }

                type Baz {
                    foo1(a: String): String
                    foo2(a: String!): String!
                    foo3(a: [String!]): [String!]
                    foo4(a: [String!]!): [String!]!
                }

                input FooInput {
                    a: [String!]!
                }"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Local_Rename_Interface_Field()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "SchemaName",
            Utf8GraphQLParser.Parse(@"
                type Foo implements IFoo {
                    abc: String
                }

                interface IFoo {
                    abc: String
                }

                extend interface IFoo {
                    abc: String @rename(to: ""newName"")
                }"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Local_Rename_Interface_Field_2()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "SchemaName",
            Utf8GraphQLParser.Parse(@"
                type Foo implements IFoo & IFooExt {
                    abc: String
                    def: String
                }

                interface IFoo {
                    abc: String
                }

                interface IFooExt implements IFoo {
                    abc: String
                    def: String
                }

                extend interface IFoo {
                    abc: String @rename(to: ""newName"")
                }"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    private MergeSchema CreatePipeline()
        => new SchemaMergePipelineBuilder()
            .Use(next =>
            {
                var middleware = new PrepareDocumentsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyExtensionsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyRenamingMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Compile();
}
