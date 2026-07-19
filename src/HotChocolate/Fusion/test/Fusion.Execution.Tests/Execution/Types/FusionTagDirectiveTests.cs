using HotChocolate.Execution;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionTagDirectiveTests : FusionTestBase
{
    [Fact]
    public void Create_Should_LoadPrivateTagsAsInternalTagDirectives_When_UsingDefaultMergeBehavior()
    {
        var schema = ComposeAndLoadSchema();
        var tagDefinition = schema.DirectiveDefinitions["tag"];
        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var fieldDirectives = query.Fields["field"].Directives;
        var enumType = schema.Types.GetType<FusionEnumTypeDefinition>("TaggedEnum");
        var inputType = schema.Types.GetType<FusionInputObjectTypeDefinition>("TaggedInput");
        var locations = new[]
        {
            new { Name = "Schema", Directives = schema.Directives },
            new { Name = "Object", Directives = query.Directives },
            new { Name = "Field", Directives = fieldDirectives },
            new
            {
                Name = "Argument",
                Directives = query.Fields["field"].Arguments["arg"].Directives
            },
            new
            {
                Name = "Tagged object",
                Directives = schema.Types.GetType<FusionObjectTypeDefinition>("Tagged").Directives
            },
            new
            {
                Name = "Interface",
                Directives = schema.Types.GetType<FusionInterfaceTypeDefinition>("TaggedInterface").Directives
            },
            new
            {
                Name = "Union",
                Directives = schema.Types.GetType<FusionUnionTypeDefinition>("TaggedUnion").Directives
            },
            new
            {
                Name = "Scalar",
                Directives = schema.Types.GetType<FusionScalarTypeDefinition>("TaggedScalar").Directives
            },
            new { Name = "Enum", Directives = enumType.Directives },
            new { Name = "Enum value", Directives = enumType.Values["VALUE"].Directives },
            new { Name = "Input object", Directives = inputType.Directives },
            new { Name = "Input field", Directives = inputType.Fields["value"].Directives }
        };

        new
        {
            Definition = new
            {
                tagDefinition.Name,
                tagDefinition.IsPublic,
                HasFusionTagDefinition = schema.DirectiveDefinitions.ContainsName("fusion__tag")
            },
            FieldViews = new
            {
                Default = new
                {
                    fieldDirectives.Count,
                    ContainsTag = fieldDirectives.ContainsName("tag"),
                    Tags = GetTagNames(fieldDirectives)
                },
                WithInternals = fieldDirectives.WithInternals
                    .AsEnumerable()
                    .Select(directive => new
                    {
                        directive.Name,
                        directive.IsPublic,
                        TagName = ((StringValueNode)directive.Arguments["name"]).Value,
                        DefinitionName = directive.Definition.Name,
                        DefinitionIsPublic = directive.Definition.IsPublic,
                        UsesSchemaDefinition = ReferenceEquals(
                            tagDefinition,
                            directive.Definition)
                    })
                    .ToArray()
            },
            Locations = locations
                .Select(location => new
                {
                    location.Name,
                    PublicCount = location.Directives.Count,
                    PublicTags = GetTagNames(location.Directives),
                    AllCount = location.Directives.WithInternals.Count,
                    ContainsFusionTag = location.Directives.WithInternals.ContainsName("fusion__tag"),
                    TagsWithInternals = GetTagNames(location.Directives.WithInternals)
                })
                .ToArray()
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public void Create_Should_LoadPublicTagsAsPublicTagDirectives_When_MergeBehaviorIsInclude()
    {
        var schema = ComposeAndLoadSchema(DirectiveMergeBehavior.Include);

        AssertTags(schema.Directives, isPublic: true);

        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        AssertTags(query.Directives, isPublic: true);
        AssertTags(query.Fields["field"].Directives, isPublic: true);
        AssertTags(query.Fields["field"].Arguments["arg"].Directives, isPublic: true);

        AssertTags(
            schema.Types.GetType<FusionObjectTypeDefinition>("Tagged").Directives,
            isPublic: true);
        AssertTags(
            schema.Types.GetType<FusionInterfaceTypeDefinition>("TaggedInterface").Directives,
            isPublic: true);
        AssertTags(
            schema.Types.GetType<FusionUnionTypeDefinition>("TaggedUnion").Directives,
            isPublic: true);
        AssertTags(
            schema.Types.GetType<FusionScalarTypeDefinition>("TaggedScalar").Directives,
            isPublic: true);

        var enumType = schema.Types.GetType<FusionEnumTypeDefinition>("TaggedEnum");
        AssertTags(enumType.Directives, isPublic: true);
        AssertTags(enumType.Values["VALUE"].Directives, isPublic: true);

        var inputType = schema.Types.GetType<FusionInputObjectTypeDefinition>("TaggedInput");
        AssertTags(inputType.Directives, isPublic: true);
        AssertTags(inputType.Fields["value"].Directives, isPublic: true);

        Assert.True(schema.DirectiveDefinitions.TryGetDirective("tag", out var tagDefinition));
        Assert.True(tagDefinition.IsPublic);
        Assert.Equal("tag", tagDefinition.Name);
    }

    [Fact]
    public void Create_Should_LoadPrivateTagOnDirectiveDefinitionAsInternalTagDirective()
    {
        var document = ComposeSchemaDocument();
        var exampleDirective = Utf8GraphQLParser.Parse(
            """
            directive @example
              @fusion__tag(name: "a")
              @fusion__tag(name: "b")
              on FIELD_DEFINITION
            """).Definitions.Single();
        document = new DocumentNode([.. document.Definitions, exampleDirective]);

        var schema = FusionSchemaDefinition.Create(document);

        AssertTags(schema.DirectiveDefinitions["example"].Directives, isPublic: false);
    }

    [Fact]
    public void Create_Should_ReusePublicTagDefinition_When_BothTagFormsArePresent()
    {
        var document = ComposeSchemaDocument(DirectiveMergeBehavior.Include);
        var additionalDefinitions = Utf8GraphQLParser.Parse(
            $$"""
            directive @example
              @fusion__tag(name: "a")
              @tag(name: "public")
              @fusion__tag(name: "b")
              on FIELD_DEFINITION

            directive @fusion__tag(name: String!) repeatable on {{TagLocations}}
            """).Definitions;
        document = new DocumentNode([.. document.Definitions, .. additionalDefinitions]);

        var schema = FusionSchemaDefinition.Create(document);
        var tagDefinition = schema.DirectiveDefinitions["tag"];
        var directives = schema.DirectiveDefinitions["example"].Directives;

        Assert.True(tagDefinition.IsPublic);
        Assert.Equal("tag", tagDefinition.Name);
        Assert.Equal(["public"], GetTagNames(directives));
        Assert.Equal(["public", "a", "b"], GetTagNames(directives.WithInternals));
        Assert.Collection(
            directives.WithInternals.AsEnumerable(),
            directive => Assert.True(directive.IsPublic),
            directive => Assert.False(directive.IsPublic),
            directive => Assert.False(directive.IsPublic));
        Assert.All(
            directives.WithInternals.AsEnumerable(),
            directive => Assert.Same(tagDefinition, directive.Definition));
    }

    [Fact]
    public void Format_Should_ExposeTagDefinitionAndHideInternalApplications_When_InternalsAreExcluded()
    {
        var schema = ComposeAndLoadSchema();

        var document = SchemaFormatter.FormatAsDocument(
            schema,
            new SchemaFormatterOptions { IncludeInternalDirectives = false });

        Assert.Equal(
            ["defer", "tag"],
            document.Definitions
                .OfType<DirectiveDefinitionNode>()
                .Select(static t => t.Name.Value));
        Assert.Empty(GetAppliedDirectiveNames(document));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Introspection_Should_ExposeNormalizedTagDefinition(
        bool enableOptInFeatures)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(t => t.EnableOptInFeatures = enableOptInFeatures)
            .AddInMemoryConfiguration(ComposeSchemaDocument())
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ __schema { directives { name } } }")
                .Build(),
            TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__schema": {
                  "directives": [
                    {
                      "name": "tag"
                    },
                    {
                      "name": "defer"
                    },
                    {
                      "name": "skip"
                    },
                    {
                      "name": "include"
                    },
                    {
                      "name": "specifiedBy"
                    },
                    {
                      "name": "oneOf"
                    }
                  ]
                }
              }
            }
            """);
    }

    private static void AssertTags(
        FusionDirectiveCollection directives,
        bool isPublic)
    {
        var allDirectives = directives.WithInternals;
        var tags = allDirectives["tag"].ToArray();

        Assert.Equal(isPublic ? 2 : 0, directives.Count);
        Assert.Equal(isPublic, directives.ContainsName("tag"));
        Assert.True(allDirectives.ContainsName("tag"));
        Assert.Equal(2, tags.Length);
        Assert.Equal(
            ["a", "b"],
            tags.Select(static t => ((StringValueNode)t.Arguments["name"]).Value));
        Assert.All(tags, tag => Assert.Equal(isPublic, tag.IsPublic));
        Assert.All(tags, static tag => Assert.Equal("tag", tag.Definition.Name));
        Assert.False(allDirectives.ContainsName("fusion__tag"));
    }

    private static string[] GetTagNames(IEnumerable<FusionDirective> directives)
        => [
            .. directives
                .Where(static t => t.Name.Equals("tag", StringComparison.Ordinal))
                .Select(static t => ((StringValueNode)t.Arguments["name"]).Value)
        ];

    private static string[] GetAppliedDirectiveNames(DocumentNode document)
    {
        var names = new List<string>();

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case SchemaDefinitionNode schema:
                    names.AddRange(schema.Directives.Select(static t => t.Name.Value));
                    break;
                case ObjectTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    foreach (var field in type.Fields)
                    {
                        names.AddRange(field.Directives.Select(static t => t.Name.Value));
                        names.AddRange(field.Arguments.SelectMany(static t => t.Directives).Select(static t => t.Name.Value));
                    }
                    break;
                case InterfaceTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    foreach (var field in type.Fields)
                    {
                        names.AddRange(field.Directives.Select(static t => t.Name.Value));
                        names.AddRange(field.Arguments.SelectMany(static t => t.Directives).Select(static t => t.Name.Value));
                    }
                    break;
                case UnionTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    break;
                case ScalarTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    break;
                case EnumTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    names.AddRange(type.Values.SelectMany(static t => t.Directives).Select(static t => t.Name.Value));
                    break;
                case InputObjectTypeDefinitionNode type:
                    names.AddRange(type.Directives.Select(static t => t.Name.Value));
                    names.AddRange(type.Fields.SelectMany(static t => t.Directives).Select(static t => t.Name.Value));
                    break;
                case DirectiveDefinitionNode directive:
                    names.AddRange(directive.Directives.Select(static t => t.Name.Value));
                    names.AddRange(directive.Arguments.SelectMany(static t => t.Directives).Select(static t => t.Name.Value));
                    break;
            }
        }

        return [.. names];
    }

    private static FusionSchemaDefinition ComposeAndLoadSchema(
        DirectiveMergeBehavior? tagMergeBehavior = null)
        => FusionSchemaDefinition.Create(ComposeSchemaDocument(tagMergeBehavior));

    private static DocumentNode ComposeSchemaDocument(
        DirectiveMergeBehavior? tagMergeBehavior = null)
    {
        var sourceSchemas = new[] { new SourceSchemaText("a", SourceSchema) };
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();

        if (tagMergeBehavior is { } value)
        {
            composerOptions.Merger.TagMergeBehavior = value;
        }

        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }

    private const string SourceSchema =
        $$"""
        schema @tag(name: "a") @tag(name: "b") {
          query: Query
        }

        type Query @tag(name: "a") @tag(name: "b") {
          field(
            arg: TaggedInput @tag(name: "a") @tag(name: "b")
          ): Tagged @tag(name: "a") @tag(name: "b")
          scalar: TaggedScalar
          enumValue: TaggedEnum
          unionValue: TaggedUnion
          interfaceValue: TaggedInterface
        }

        type Tagged implements TaggedInterface @tag(name: "a") @tag(name: "b") {
          id: ID
        }

        interface TaggedInterface @tag(name: "a") @tag(name: "b") {
          id: ID
        }

        union TaggedUnion @tag(name: "a") @tag(name: "b") = Tagged

        scalar TaggedScalar @tag(name: "a") @tag(name: "b")

        enum TaggedEnum @tag(name: "a") @tag(name: "b") {
          VALUE @tag(name: "a") @tag(name: "b")
        }

        input TaggedInput @tag(name: "a") @tag(name: "b") {
          value: String @tag(name: "a") @tag(name: "b")
        }

        directive @tag(name: String!) repeatable on {{TagLocations}}
        """;

    private const string TagLocations =
        "SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | "
        + "INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | "
        + "INPUT_FIELD_DEFINITION | DIRECTIVE_DEFINITION";
}
