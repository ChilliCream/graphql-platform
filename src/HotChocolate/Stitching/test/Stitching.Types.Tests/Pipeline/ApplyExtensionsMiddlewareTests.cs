using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Stitching.Types.Pipeline;

public class ApplyExtensionsMiddlewareTests
{
    [Fact]
    public async Task Apply_Object_Extension_Single_Document()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo {
                    def: String
                }"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Object_Extension_Is_Preserved()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo {
                    abc: String
                }

                extend type Bar {
                    def: String
                }"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Object_Extension_Merge_Field_Directives_Single_Document()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo {
                    abc: String @directive
                }"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Object_Extension_Merge_Directives_Single_Document()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo @directive"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Object_Extension_Merge_Directives_2_Single_Document()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo @a {
                    abc: String
                }

                extend type Foo @b"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Object_Extension_Field_Type_Mismatch()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo {
                    abc: Int
                }"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        async Task Error() => await middleware.InvokeAsync(context);

        // assert
        await Assert.ThrowsAsync<GraphQLException>(Error);
    }

    [Fact]
    public async Task Apply_Local_Remove()
    {
        // arrange
        var middleware = new ApplyExtensionsMiddleware(_ => default);

        var service = new ServiceConfiguration(
            "abc",
            Parse(@"
                type Foo @a {
                    abc: String
                }

                extend type Foo {
                    abc: String @remove
                }"));
        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await middleware.InvokeAsync(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }
}
