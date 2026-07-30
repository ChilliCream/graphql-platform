using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLResourceModelTests
{
    [Fact]
    public void GetReferencedSourceSchemas_Should_ReturnSharedResourceAndDeclaration()
    {
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaExport(
                "Products",
                "Release",
                "net9.0",
                runtimeIdentifier: null,
                TimeSpan.FromMinutes(1));
        var unrelated = builder
            .AddProject("unrelated", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        var unreferenced = builder
            .AddProject("unreferenced", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithReference(products)
            .WithReference(unrelated)
            .WithGraphQLSchemaComposition();
        var model = new DistributedApplicationModel(
            [
                products.Resource,
                unrelated.Resource,
                unreferenced.Resource,
                gateway.Resource
            ]);

        var sources = GraphQLResourceModel.GetReferencedSourceSchemas(
            gateway.Resource,
            model);

        Assert.Collection(
            sources,
            source =>
            {
                Assert.Same(products.Resource, source.Resource);
                Assert.Same(
                    GraphQLResourceModel.GetSourceSchema(products.Resource),
                    source.Declaration);
            },
            source =>
            {
                Assert.Same(unrelated.Resource, source.Resource);
                Assert.Same(
                    GraphQLResourceModel.GetSourceSchema(unrelated.Resource),
                    source.Declaration);
            });
    }

    [Fact]
    public void GetReferencedSourceSchemas_Should_FailClosed_When_DeclarationIsAmbiguous()
    {
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        products.Resource.Annotations.Add(
            new GraphQLSourceSchemaAnnotation
            {
                Location = SourceSchemaLocationType.SchemaEndpoint
            });
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithReference(products)
            .WithGraphQLSchemaComposition();
        var model = new DistributedApplicationModel(
            [products.Resource, gateway.Resource]);

        Assert.Throws<InvalidOperationException>(
            () => GraphQLResourceModel.GetReferencedSourceSchemas(
                gateway.Resource,
                model));
    }

    [Fact]
    public void GetProjectSchemaPaths_Should_ResolveSettingsBesideNestedSchema()
    {
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaFile(
                System.IO.Path.Combine("schemas", "products.graphqls"),
                "Products");
        var declaration = GraphQLResourceModel.GetSourceSchema(products.Resource);

        var paths = GraphQLResourceModel.GetProjectSchemaPaths(
            products.Resource,
            declaration);

        var projectDirectory = System.IO.Path.GetDirectoryName(GetTestProjectFile())!;
        $$"""
        Project: {{System.IO.Path.GetRelativePath(projectDirectory, paths.ProjectPath)}}
        Schema: {{System.IO.Path.GetRelativePath(projectDirectory, paths.SchemaPath)}}
        Settings: {{System.IO.Path.GetRelativePath(projectDirectory, paths.SettingsPath)}}
        Extensions: {{System.IO.Path.GetRelativePath(projectDirectory, paths.ExtensionsPath)}}
        """.MatchInlineSnapshot(
            """
            Project: HotChocolate.Fusion.Aspire.Tests.csproj
            Schema: schemas/products.graphqls
            Settings: schemas/products-settings.json
            Extensions: schemas/products-extensions.graphqls
            """);
    }

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");
}
