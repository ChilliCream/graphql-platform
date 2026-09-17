using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class DocumentNormalizationMiddlewareTests
{
    [Fact]
    public async Task Normalizes_Document_By_Inlining_Fragments()
    {
        // arrange
        var capturedDocuments = new List<DocumentNode?>();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("foo-value");
                d.Field("bar").Resolve("bar-value");
            })
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    capturedDocuments.Add(context.OperationDocumentInfo.NormalizedDocument);
                    await next(context);
                },
                key: "CaptureNormalizedDocument",
                after: WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query Test {
              foo
              ...F
            }

            fragment F on Query {
              bar
            }
            """;

        // act
        var result = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);

        var normalizedDocument = capturedDocuments.Single()!;
        var fieldNames = ((OperationDefinitionNode)normalizedDocument.Definitions.Single())
            .SelectionSet.Selections
            .OfType<FieldNode>()
            .Select(f => f.Name.Value)
            .ToArray();

        // the fragment spread was inlined into the operation's own selection set.
        Assert.Equal(["foo", "bar"], fieldNames);
    }

    [Fact]
    public async Task Warm_Cache_Skips_Rewrite_And_Reuses_The_Same_Normalized_Document()
    {
        // arrange
        var capturedDocuments = new List<DocumentNode?>();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("foo-value"))
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    capturedDocuments.Add(context.OperationDocumentInfo.NormalizedDocument);
                    await next(context);
                },
                key: "CaptureNormalizedDocument",
                after: WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query WarmCache {
              foo
            }
            """;

        // act
        await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);

        // assert: the second execution hit the normalized-document cache instead of
        // rewriting the document again, so both requests observe the very same instance.
        Assert.Equal(2, capturedDocuments.Count);
        Assert.Same(capturedDocuments[0], capturedDocuments[1]);

        var normalizedDocumentCache = executor.Schema.Services.GetRequiredService<NormalizedDocumentCache>();
        Assert.Equal(1, normalizedDocumentCache.Count);
    }

    [Fact]
    public async Task Multi_Operation_Document_Normalizes_The_Selected_Operation_Per_Operation()
    {
        // arrange
        var capturedDocuments = new List<DocumentNode?>();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("foo-value");
                d.Field("bar").Resolve("bar-value");
            })
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    capturedDocuments.Add(context.OperationDocumentInfo.NormalizedDocument);
                    await next(context);
                },
                key: "CaptureNormalizedDocument",
                after: WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string documentText =
            """
            query A {
              foo
            }

            query B {
              bar
            }
            """;

        // act
        var resultA = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(documentText).SetOperationName("A").Build(),
            TestContext.Current.CancellationToken);
        var resultB = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(documentText).SetOperationName("B").Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(resultA.ExpectOperationResult().Errors);
        Assert.Empty(resultB.ExpectOperationResult().Errors);

        var selectedOperations = capturedDocuments
            .Select(document =>
            {
                var operation = (OperationDefinitionNode)document!.Definitions.Single();
                var fieldName = ((FieldNode)operation.SelectionSet.Selections.Single()).Name.Value;
                return (Name: operation.Name?.Value, Field: fieldName);
            })
            .ToArray();

        // each operation was normalized on its own, keeping only its own selection.
        Assert.Equal([("A", "foo"), ("B", "bar")], selectedOperations);

        // the two operations in the same document are cached under distinct operation ids.
        var normalizedDocumentCache = executor.Schema.Services.GetRequiredService<NormalizedDocumentCache>();
        Assert.Equal(2, normalizedDocumentCache.Count);
    }

    [Fact]
    public async Task Missing_Document_Returns_State_Invalid_Error_Instead_Of_Throwing()
    {
        // arrange
        // A custom pipeline that reaches document normalization without a document parser
        // stage must fail with the ordinary state-invalid request error, not throw.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("foo-value"))
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentNormalization()
            .UseOperationExecution()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorHelper.StateInvalidForOperationResolver().Errors[0].Message, error.Message);
    }

    [Fact]
    public async Task Not_Validated_Document_Returns_State_Invalid_Error_Instead_Of_Throwing()
    {
        // arrange
        // A custom pipeline that orders document normalization before document validation
        // must fail with the ordinary state-invalid request error, not throw.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("foo-value"))
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentParser()
            .UseDocumentNormalization()
            .UseOperationExecution()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorHelper.StateInvalidForOperationResolver().Errors[0].Message, error.Message);
    }

    [Fact]
    public async Task Empty_Document_Id_Returns_State_Invalid_Error_Instead_Of_Throwing()
    {
        // arrange
        // A custom pipeline stage can hand document normalization a validated document without
        // ever assigning it an id; that must fail with the ordinary state-invalid request
        // error, not throw.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("foo-value"))
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseRequest(
                (_, next) => context =>
                {
                    context.OperationDocumentInfo.Document = Utf8GraphQLParser.Parse("{ foo }");
                    context.OperationDocumentInfo.IsValidated = true;
                    return next(context);
                },
                key: "SeedValidatedDocumentWithoutId")
            .UseDocumentNormalization()
            .UseOperationExecution()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorHelper.StateInvalidForOperationResolver().Errors[0].Message, error.Message);
    }
}
