using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;

namespace HotChocolate.ModelContextProtocol.Factories;

public sealed class OperationToolFactoryTests
{
    [Fact]
    public void CreateTool_DocumentWithNoOperations_ThrowsException()
    {
        // arrange & act
        static OperationTool Action()
        {
            var schema = CreateSchema();
            var document = Utf8GraphQLParser.Parse("fragment Fragment on Type { field }");
            var toolDefinition = new OperationToolDefinition("tool", document);

            return new OperationToolFactory(schema).CreateTool(toolDefinition);
        }

        // assert
        Assert.Equal(
            "An operation tool document must have exactly one operation definition. (Parameter 'document')",
            Assert.Throws<ArgumentException>(Action).Message);
    }

    [Fact]
    public void CreateTool_DocumentWithMultipleOperations_ThrowsException()
    {
        // arrange & act
        static OperationTool Action()
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
            var toolDefinition = new OperationToolDefinition("tool", document);

            return new OperationToolFactory(schema).CreateTool(toolDefinition);
        }

        // assert
        Assert.Equal(
            "An operation tool document must have exactly one operation definition. (Parameter 'document')",
            Assert.Throws<ArgumentException>(Action).Message);
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
        var toolDefinition = new OperationToolDefinition("get_books", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        Assert.Equal("get_books", mcpTool.Name);
        Assert.Equal("Get Books", mcpTool.Title);
        Assert.Equal("Get books", mcpTool.Description);
        Assert.Equal(false, mcpTool.Annotations?.DestructiveHint);
        Assert.Equal(true, mcpTool.Annotations?.IdempotentHint);
        Assert.Equal(true, mcpTool.Annotations?.OpenWorldHint);
        Assert.Equal(true, mcpTool.Annotations?.ReadOnlyHint);
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
        var toolDefinition = new OperationToolDefinition("add_book", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        Assert.Equal("add_book", mcpTool.Name);
        Assert.Equal("Add Book", mcpTool.Title);
        Assert.Equal("Add book", mcpTool.Description);
        Assert.Equal(true, mcpTool.Annotations?.DestructiveHint);
        Assert.Equal(false, mcpTool.Annotations?.IdempotentHint);
        Assert.Equal(true, mcpTool.Annotations?.OpenWorldHint);
        Assert.Equal(false, mcpTool.Annotations?.ReadOnlyHint);
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
        var toolDefinition = new OperationToolDefinition("book_added", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        Assert.Equal("book_added", mcpTool.Name);
        Assert.Equal("Book Added", mcpTool.Title);
        Assert.Equal("Book added", mcpTool.Description);
        Assert.Equal(false, mcpTool.Annotations?.DestructiveHint);
        Assert.Equal(true, mcpTool.Annotations?.IdempotentHint);
        Assert.Equal(true, mcpTool.Annotations?.OpenWorldHint);
        Assert.Equal(true, mcpTool.Annotations?.ReadOnlyHint);
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
        var toolDefinition = new OperationToolDefinition("get_books", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal("Custom Title", tool.Tool.Title);
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
        var toolDefinition = new OperationToolDefinition("add_book", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        Assert.Equal(false, mcpTool.Annotations?.DestructiveHint);
        Assert.Equal(true, mcpTool.Annotations?.IdempotentHint);
        Assert.Equal(false, mcpTool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public void CreateTool_WithNullableVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithNullableVariables.graphql"));
        var toolDefinition = new OperationToolDefinition("get_with_nullable_variables", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        mcpTool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        mcpTool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithNonNullableVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithNonNullableVariables.graphql"));
        var toolDefinition = new OperationToolDefinition("get_with_non_nullable_variables", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        mcpTool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        mcpTool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithDefaultedVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithDefaultedVariables.graphql"));
        var toolDefinition = new OperationToolDefinition("get_with_defaulted_variables", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        mcpTool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        mcpTool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
    }

    [Fact]
    public void CreateTool_WithComplexVariables_CreatesCorrectSchema()
    {
        // arrange
        var schema = CreateSchema(s => s.AddType(new TimeSpanType(TimeSpanFormat.DotNet)));
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithComplexVariables.graphql"));
        var toolDefinition = new OperationToolDefinition("get_with_complex_variables", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        mcpTool.InputSchema.MatchSnapshot(postFix: "Input", extension: ".json");
        mcpTool.OutputSchema.MatchSnapshot(postFix: "Output", extension: ".json");
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
        var toolDefinition = new OperationToolDefinition("get_with_variable_min_max_values", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        tool.Tool.InputSchema.MatchSnapshot(extension: ".json");
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
        var toolDefinition = new OperationToolDefinition("get_with_interface_type", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        tool.Tool.OutputSchema.MatchSnapshot(extension: ".json");
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
        var toolDefinition = new OperationToolDefinition("get_with_union_type", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        tool.Tool.OutputSchema.MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void CreateTool_WithSkipAndInclude_CreatesCorrectOutputSchema()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(
            File.ReadAllText("__resources__/GetWithSkipAndInclude.graphql"));
        var toolDefinition = new OperationToolDefinition("get_with_skip_and_include", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        tool.Tool.OutputSchema.MatchSnapshot(extension: ".json");
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(destructiveHint, tool.Tool.Annotations?.DestructiveHint);
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(destructiveHint, tool.Tool.Annotations?.DestructiveHint);
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(destructiveHint, tool.Tool.Annotations?.DestructiveHint);
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(idempotentHint, tool.Tool.Annotations?.IdempotentHint);
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(idempotentHint, tool.Tool.Annotations?.IdempotentHint);
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
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(idempotentHint, tool.Tool.Annotations?.IdempotentHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldSubfieldTool.graphql", false)]
    public void CreateTool_McpToolAnnotationsOpenWorldHintImplementationFirst_SetsCorrectHint(
        string fileName,
        bool openWorldHint)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(openWorldHint, tool.Tool.Annotations?.OpenWorldHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
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
                            .Field("explicitClosedWorldSubfieldQuery")
                            .Type(typeof(TestSchema.ExplicitClosedWorld))
                            .McpToolAnnotations(openWorldHint: false);
                    })
                .Use(next => next)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(openWorldHint, tool.Tool.Annotations?.OpenWorldHint);
    }

    [Theory]
    [InlineData("ImplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitOpenWorldTool.graphql", true)]
    [InlineData("ExplicitClosedWorldTool.graphql", false)]
    [InlineData("ExplicitOpenWorldSubfieldTool.graphql", true)]
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
                        explicitClosedWorldSubfieldQuery: ExplicitClosedWorld
                            @mcpToolAnnotations(openWorldHint: false)
                    }

                    type ExplicitOpenWorld {
                        explicitOpenWorldField: Int @mcpToolAnnotations(openWorldHint: true)
                    }

                    type ExplicitClosedWorld {
                        explicitClosedWorldField: Int @mcpToolAnnotations(openWorldHint: false)
                    }
                    """)
                .Use(next => next)
                .Create();
        var document = Utf8GraphQLParser.Parse(File.ReadAllText($"__resources__/{fileName}"));
        var toolDefinition = new OperationToolDefinition("tool", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);

        // assert
        Assert.Equal(openWorldHint, tool.Tool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public void CreateTool_McpToolAnnotationsWithFragment_SetsCorrectHints()
    {
        // arrange
        var schema = CreateSchema();
        var document =
            Utf8GraphQLParser.Parse(
                File.ReadAllText("__resources__/AnnotationsWithFragment.graphql"));
        var toolDefinition = new OperationToolDefinition("annotations_with_fragment", document);

        // act
        var tool = new OperationToolFactory(schema).CreateTool(toolDefinition);
        var mcpTool = tool.Tool;

        // assert
        Assert.Equal(true, mcpTool.Annotations?.DestructiveHint);
        Assert.Equal(false, mcpTool.Annotations?.IdempotentHint);
        Assert.Equal(true, mcpTool.Annotations?.OpenWorldHint);
    }

    private static Schema CreateSchema(Action<ISchemaBuilder>? configure = null)
    {
        var schemaBuilder =
            SchemaBuilder
                .New()
                .AddAuthorizeDirectiveType()
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
