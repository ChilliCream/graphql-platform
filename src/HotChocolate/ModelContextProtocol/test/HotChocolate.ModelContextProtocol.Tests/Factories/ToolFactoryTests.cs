using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.Types;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol.Factories;

public sealed class ToolFactoryTests
{
    [Fact]
    public void CreateTool_DocumentWithNoOperations_ThrowsException()
    {
        // arrange & act
        static Tool Action()
        {
            var schema = CreateSchema();
            var document = Utf8GraphQLParser.Parse("fragment Fragment on Type { field }");

            return new ToolFactory(schema).CreateTool("", document);
        }

        // assert
        Assert.Throws<InvalidOperationException>(Action);
    }

    [Fact]
    public void CreateTool_DocumentWithMultipleOperations_ThrowsException()
    {
        // arrange & act
        static Tool Action()
        {
            var schema = CreateSchema();
            var document = Utf8GraphQLParser.Parse(
                """
                query Operation1 {
                    query {
                        field
                    }
                }

                query Operation2 {
                    query {
                        field
                    }
                }
                """);

            return new ToolFactory(schema).CreateTool("", document);
        }

        // assert
        Assert.Throws<InvalidOperationException>(Action);
    }

    [Fact]
    public void CreateTool_ValidQueryDocument_ReturnsTool()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            "Get books"
            query GetBooks {
                books {
                    title
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("get_books", document);

        // assert
        Assert.Equal("get_books", tool.Name);
        Assert.Equal("Get Books", tool.Title);
        Assert.Equal("Get books", tool.Description);
        Assert.Equal(false, tool.Annotations?.DestructiveHint);
        Assert.Equal(true, tool.Annotations?.IdempotentHint);
        Assert.Equal(true, tool.Annotations?.OpenWorldHint);
        Assert.Equal(true, tool.Annotations?.ReadOnlyHint);
    }

    [Fact]
    public void CreateTool_ValidMutationDocument_ReturnsTool()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            "Add book"
            mutation AddBook {
                addBook {
                    title
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("add_book", document);

        // assert
        Assert.Equal("add_book", tool.Name);
        Assert.Equal("Add Book", tool.Title);
        Assert.Equal("Add book", tool.Description);
        Assert.Equal(true, tool.Annotations?.DestructiveHint);
        Assert.Equal(false, tool.Annotations?.IdempotentHint);
        Assert.Equal(true, tool.Annotations?.OpenWorldHint);
        Assert.Equal(false, tool.Annotations?.ReadOnlyHint);
    }

    [Fact]
    public void CreateTool_ValidSubscriptionDocument_ReturnsTool()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            "Book added"
            subscription BookAdded {
                bookAdded {
                    title
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("book_added", document);

        // assert
        Assert.Equal("book_added", tool.Name);
        Assert.Equal("Book Added", tool.Title);
        Assert.Equal("Book added", tool.Description);
        Assert.Equal(false, tool.Annotations?.DestructiveHint);
        Assert.Equal(true, tool.Annotations?.IdempotentHint);
        Assert.Equal(true, tool.Annotations?.OpenWorldHint);
        Assert.Equal(true, tool.Annotations?.ReadOnlyHint);
    }

    [Fact]
    public void CreateTool_SetTitle_ReturnsTool()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            query GetBooks @mcpTool(title: "Custom Title") {
                books {
                    title
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("get_books", document);

        // assert
        Assert.Equal("Custom Title", tool.Title);
    }

    [Fact]
    public void CreateTool_SetAnnotations_ReturnsTool()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            mutation AddBook
                @mcpTool(destructiveHint: false, idempotentHint: true, openWorldHint: false) {
                addBook {
                    title
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("add_book", document);

        // assert
        Assert.Equal(false, tool.Annotations?.DestructiveHint);
        Assert.Equal(true, tool.Annotations?.IdempotentHint);
        Assert.Equal(false, tool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public void CreateTool_WithNullableVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithNullableVariables.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_nullable_variables", document);

        // assert
        tool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        tool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithNonNullableVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithNonNullableVariables.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_non_nullable_variables", document);

        // assert
        tool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        tool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithDefaultedVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithDefaultedVariables.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_defaulted_variables", document);

        // assert
        tool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        tool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithComplexVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema(s => s.AddType(new TimeSpanType(TimeSpanFormat.DotNet)));
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithComplexVariables.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_complex_variables", document);

        // assert
        tool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        tool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithVariableMinMaxValues_CreatesCorrectInputSchema()
    {
        // arrange
        var schema =
            CreateSchema(
                s => s
                    .AddType(new ByteType(min: 1, max: 10))
                    .AddType(new DecimalType(min: 1, max: 10))
                    .AddType(new FloatType(min: 1.0, max: 10.0))
                    .AddType(new IntType(min: 1, max: 10))
                    .AddType(new LongType(min: 1, max: 10))
                    .AddType(new ShortType(min: 1, max: 10)));
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithVariableMinMaxValues.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_variable_min_max_values", document);

        // assert
        tool.InputSchema.MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void CreateTool_WithInterfaceType_CreatesCorrectOutputSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            query GetWithInterfaceType {
                withInterfaceType {
                    __typename
                    name
                    ... on Cat {
                        isPurring
                    }
                    ... on Dog {
                        isBarking
                    }
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_interface_type", document);

        // assert
        tool.OutputSchema.MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void CreateTool_WithUnionType_CreatesCorrectOutputSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            query GetWithUnionType {
                withUnionType {
                    __typename
                    ... on Cat {
                        isPurring
                    }
                    ... on Dog {
                        isBarking
                    }
                }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_union_type", document);

        // assert
        tool.OutputSchema.MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void CreateTool_WithSkipAndInclude_CreatesCorrectOutputSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            query GetWithSkipAndInclude($skip: Boolean!, $include: Boolean!) {
                withDefaultedVariables {
                    # Skip
                    skipped: int @skip(if: true)
                    notSkipped: int @skip(if: false)
                    possiblySkipped: int @skip(if: $skip)
                    # Include
                    included: int @include(if: true)
                    notIncluded: int @include(if: false)
                    possiblyIncluded: int @include(if: $include)
                    # Skip and Include (excluded)
                    skippedAndIncluded: int @skip(if: true) @include(if: true)
                    skippedAndNotIncluded: int @skip(if: true) @include(if: false)
                    skippedAndPossiblyIncluded: int @skip(if: true) @include(if: $include)
                    notSkippedAndNotIncluded: int @skip(if: false) @include(if: false)
                    possiblySkippedAndNotIncluded: int @skip(if: $skip) @include(if: false)
                    # Skip and Include (included)
                    notSkippedAndIncluded: int @skip(if: false) @include(if: true)
                    notSkippedAndPossiblyIncluded: int @skip(if: false) @include(if: $include)
                    possiblySkippedAndIncluded: int @skip(if: $skip) @include(if: true)
                    possiblySkippedAndPossiblyIncluded: int
                        @skip(if: $skip)
                        @include(if: $include)
                    # Object field (nested fields are still required)
                    objectFieldPossiblySkipped: object @skip(if: $skip) {
                        field1A { field1B { field1C } }
                    }
                    # Fragment spread
                    ...Fragment @skip(if: $skip)
                    # Inline fragment
                    ... @skip(if: $skip) {
                        inlineFragmentFieldPossiblySkipped: int
                    }
                }
            }

            fragment Fragment on Object1Defaulted {
                field1A { field1B { field1C } }
            }
            """);

        // act
        var tool = new ToolFactory(schema).CreateTool("get_with_skip_and_include", document);

        // assert
        tool.OutputSchema.MatchSnapshot(extension: ".json");
    }

    [Theory]
    [InlineData("ImplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitNonDestructiveTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsDestructiveHintImplementationFirst_SetsCorrectHint(
        string fileName,
        bool destructiveHint)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(destructiveHint, tool.Annotations?.DestructiveHint);
    }

    [Theory]
    [InlineData("ImplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitNonDestructiveTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsCodeFirst_SetsCorrectHint(
        string fileName,
        bool destructiveHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddMutationType(
                    descriptor =>
                    {
                        descriptor.Name(OperationTypeNames.Mutation);

                        descriptor
                            .Field("implicitDestructiveMutation")
                            .Type<IntType>();

                        descriptor
                            .Field("explicitDestructiveMutation")
                            .Type<IntType>()
                            .McpToolAnnotations(destructiveHint: true);

                        descriptor
                            .Field("explicitNonDestructiveMutation")
                            .Type<IntType>()
                            .McpToolAnnotations(destructiveHint: false);
                    })
                .Use(next => next)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(destructiveHint, tool.Annotations?.DestructiveHint);
    }

    [Theory]
    [InlineData("ImplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitDestructiveTool.graphql", true)]
    [InlineData("ExplicitNonDestructiveTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsDestructiveHintSchemaFirst_SetsCorrectHint(
        string fileName,
        bool destructiveHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddDocumentFromString(
                    """
                    type Mutation {
                        implicitDestructiveMutation: Int
                        explicitDestructiveMutation: Int
                            @mcpToolAnnotations(destructiveHint: true)
                        explicitNonDestructiveMutation: Int
                            @mcpToolAnnotations(destructiveHint: false)
                    }
                    """)
                .Use(next => next)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(destructiveHint, tool.Annotations?.DestructiveHint);
    }

    [Theory]
    [InlineData("ImplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitIdempotentTool.graphql", true)]
    public void CreateTool_McpToolAnnotationsIdempotentHintImplementationFirst_SetsCorrectHint(
        string fileName,
        bool idempotentHint)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(idempotentHint, tool.Annotations?.IdempotentHint);
    }

    [Theory]
    [InlineData("ImplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitIdempotentTool.graphql", true)]
    public void CreateTool_McpToolAnnotationsIdempotentHintCodeFirst_SetsCorrectHint(
        string fileName,
        bool idempotentHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddMutationType(
                    descriptor =>
                    {
                        descriptor.Name(OperationTypeNames.Mutation);

                        descriptor
                            .Field("implicitNonIdempotentMutation")
                            .Type<IntType>();

                        descriptor
                            .Field("explicitNonIdempotentMutation")
                            .Type<IntType>()
                            .McpToolAnnotations(idempotentHint: false);

                        descriptor
                            .Field("explicitIdempotentMutation")
                            .Type<IntType>()
                            .McpToolAnnotations(idempotentHint: true);
                    })
                .Use(next => next)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(idempotentHint, tool.Annotations?.IdempotentHint);
    }

    [Theory]
    [InlineData("ImplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitNonIdempotentTool.graphql", false)]
    [InlineData("ExplicitIdempotentTool.graphql", true)]
    public void CreateTool_McpToolAnnotationsIdempotentHintSchemaFirst_SetsCorrectHint(
        string fileName,
        bool idempotentHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddDocumentFromString(
                    """
                    type Mutation {
                        implicitNonIdempotentMutation: Int
                        explicitNonIdempotentMutation: Int
                            @mcpToolAnnotations(idempotentHint: false)
                        explicitIdempotentMutation: Int
                            @mcpToolAnnotations(idempotentHint: true)
                    }
                    """)
                .Use(next => next)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(idempotentHint, tool.Annotations?.IdempotentHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
    [InlineData("ImplicitClosedWorldSubfieldTool.graphql", false)]
    [InlineData("ExplicitClosedWorldSubfieldTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsOpenWorldHintImplementationFirst_SetsCorrectHint(
        string fileName,
        bool openWorldHint)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(openWorldHint, tool.Annotations?.OpenWorldHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
    [InlineData("ImplicitClosedWorldSubfieldTool.graphql", false)]
    [InlineData("ExplicitClosedWorldSubfieldTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsOpenWorldHintCodeFirst_SetsCorrectHint(
        string fileName,
        bool openWorldHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddQueryType(
                    descriptor =>
                    {
                        descriptor.Name(OperationTypeNames.Query);

                        descriptor
                            .Field("implicitOpenWorldQuery")
                            .Type<IntType>();

                        descriptor
                            .Field("explicitOpenWorldQuery")
                            .Type<IntType>()
                            .McpToolAnnotations(openWorldHint: true);

                        descriptor
                            .Field("explicitClosedWorldQuery")
                            .Type<IntType>()
                            .McpToolAnnotations(openWorldHint: false);

                        descriptor
                            .Field("explicitOpenWorldSubfieldQuery")
                            .Type(typeof(TestSchema.ExplicitOpenWorld))
                            .McpToolAnnotations(openWorldHint: false);

                        descriptor
                            .Field("implicitClosedWorldSubfieldQuery")
                            .Type(typeof(TestSchema.ImplicitClosedWorld))
                            .McpToolAnnotations(openWorldHint: false);

                        descriptor
                            .Field("explicitClosedWorldSubfieldQuery")
                            .Type(typeof(TestSchema.ExplicitClosedWorld))
                            .McpToolAnnotations(openWorldHint: false);
                    })
                .Use(next => next)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(openWorldHint, tool.Annotations?.OpenWorldHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
    [InlineData("ImplicitClosedWorldSubfieldTool.graphql", false)]
    [InlineData("ExplicitClosedWorldSubfieldTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsOpenWorldHintSchemaFirst_SetsCorrectHint(
        string fileName,
        bool openWorldHint)
    {
        // arrange
        var schema =
            SchemaBuilder
                .New()
                .AddMcp()
                .AddDocumentFromString(
                    """
                    type Query {
                        implicitOpenWorldQuery: Int
                        explicitOpenWorldQuery: Int
                            @mcpToolAnnotations(openWorldHint: true)
                        explicitClosedWorldQuery: Int
                            @mcpToolAnnotations(openWorldHint: false)
                        explicitOpenWorldSubfieldQuery: ExplicitOpenWorld
                            @mcpToolAnnotations(openWorldHint: false)
                        implicitClosedWorldSubfieldQuery: ImplicitClosedWorld
                            @mcpToolAnnotations(openWorldHint: false)
                        explicitClosedWorldSubfieldQuery: ExplicitClosedWorld
                            @mcpToolAnnotations(openWorldHint: false)
                    }

                    type ExplicitOpenWorld {
                        explicitOpenWorldField: Int @mcpToolAnnotations(openWorldHint: true)
                    }

                    type ImplicitClosedWorld {
                        implicitClosedWorldField: Int
                    }

                    type ExplicitClosedWorld {
                        explicitClosedWorldField: Int @mcpToolAnnotations(openWorldHint: false)
                    }
                    """)
                .Use(next => next)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(openWorldHint, tool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public void CreateTool_McpToolAnnotationsWithFragment_SetsCorrectHints()
    {
        // arrange
        var schema = CreateSchema();
        var document =
            Utf8GraphQLParser.Parse(
                File.ReadAllText("__resources__/AnnotationsWithFragment.graphql"));

        // act
        var tool = new ToolFactory(schema).CreateTool("", document);

        // assert
        Assert.Equal(true, tool.Annotations?.DestructiveHint);
        Assert.Equal(false, tool.Annotations?.IdempotentHint);
        Assert.Equal(true, tool.Annotations?.OpenWorldHint);
    }

    private static Schema CreateSchema(Action<ISchemaBuilder>? configure = null)
    {
        var schemaBuilder =
            SchemaBuilder
                .New()
                .ModifyOptions(o => o.StripLeadingIFromInterface = true)
                .AddMcp()
                .AddQueryType<TestSchema.Query>()
                .AddMutationType<TestSchema.Mutation>()
                .AddSubscriptionType<TestSchema.Subscription>()
                .AddInterfaceType<TestSchema.IPet>()
                .AddUnionType<TestSchema.IPet>()
                .AddObjectType<TestSchema.Cat>()
                .AddObjectType<TestSchema.Dog>();

        configure?.Invoke(schemaBuilder);

        return schemaBuilder.Create();
    }
}
