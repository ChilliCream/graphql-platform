using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities;

public class SchemaHelperTests
{
    [Fact]
    public void LoadGitHubSchema()
    {
        // arrange
        var schemaSdl = FileResource.Open("GitHub.graphql");
        const string extensionsSdl = @"extend schema @key(fields: ""id"")";

        // act
        var schema =
            SchemaHelper.Load(
                [
                    new("GitHub.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                    new("GitHub.extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl))
                ]);

        // assert
        var scalarType = schema.Types.GetType<ScalarType>("X509Certificate");

        Assert.Equal(
            "global::System.String",
            scalarType.GetSerializationType());

        Assert.Equal(
            "global::System.String",
            scalarType.GetRuntimeType());
    }

    [Fact]
    public void Load_WithInputObjectTypeExtension_ForwardsExtensionFieldToBuilder()
    {
        // arrange
        const string schemaSdl = """
            type Query { noop: String }
            input FooInput { value: String }
            """;
        const string extensionSdl = """
            extend schema @key(fields: "id")
            extend input FooInput { extra: String }
            """;

        // act
        var schema = SchemaHelper.Load(
            [
                new("schema.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                new("schema.extensions.graphql", Utf8GraphQLParser.Parse(extensionSdl))
            ]);

        // assert — the extension field reached the builder
        var inputType = schema.Types.GetType<InputObjectType>("FooInput");
        Assert.True(inputType.Fields.ContainsField("extra"),
            "The InputObjectTypeExtensionNode must be forwarded to the schema builder");
    }

    [Fact]
    public void Load_WithRenameOnExtendedInputField_CanResolveRenameDirectiveValue()
    {
        // arrange — @rename applied to an extended field (the typical use case for FIX-02)
        const string schemaSdl = """
            type Query { noop: String }
            input FooInput { value: String }
            """;
        const string extensionSdl = """
            extend schema @key(fields: "id")
            extend input FooInput { extra: String @rename(name: "ExtraRenamed") }
            """;

        // act
        var schema = SchemaHelper.Load(
            [
                new("schema.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                new("schema.extensions.graphql", Utf8GraphQLParser.Parse(extensionSdl))
            ]);

        // assert — FIX-02: RenameDirectiveType is registered so the directive resolves to
        // a typed RenameDirective POCO (without the fix, FirstOrDefault<RenameDirective>() returns null)
        var inputType = schema.Types.GetType<InputObjectType>("FooInput");
        var field = inputType.Fields["extra"];
        var renameDirective = field.Directives.FirstOrDefault<RenameDirective>()?.ToValue<RenameDirective>();
        Assert.NotNull(renameDirective);
        Assert.Equal("ExtraRenamed", renameDirective.Name);
    }

    [Fact]
    public void Load_WithExplicitRenameDirectiveDeclaration_DoesNotThrowDuplicateError()
    {
        // arrange — schema explicitly declares @rename (as real user schemas do)
        const string schemaSdl = """
            type Query { noop: String }
            directive @rename(name: String!) on INPUT_FIELD_DEFINITION | INPUT_OBJECT | ENUM | ENUM_VALUE
            """;
        const string extensionSdl = """extend schema @key(fields: "id")""";

        // act + assert — must not throw SchemaException for duplicate directive
        var exception = Record.Exception(() =>
            SchemaHelper.Load(
                [
                    new("schema.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                    new("schema.extensions.graphql", Utf8GraphQLParser.Parse(extensionSdl))
                ]));

        Assert.Null(exception);
    }
}
