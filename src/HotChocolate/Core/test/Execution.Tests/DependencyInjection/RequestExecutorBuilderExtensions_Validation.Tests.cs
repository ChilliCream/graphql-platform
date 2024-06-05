using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsValidationTests
{
    [Fact]
    public void AddValidationVisitor_1_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddValidationVisitor<MockVisitor>(null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddValidationVisitor_2_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddValidationVisitor<MockVisitor>(
                null!,
                (_, _) => throw new NotImplementedException());

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddValidationVisitor_2_Factory_Is_Null()
    {
        void Fail() => new ServiceCollection()
            .AddGraphQL()
            .AddValidationVisitor<MockVisitor>(null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddValidationRuler_1_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddValidationRule<MockRule>(null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddValidationRule_2_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddValidationRule<MockRule>(
                null!,
                (_, _) => throw new NotImplementedException());

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddValidationRule_2_Factory_Is_Null()
    {
        void Fail() => new ServiceCollection()
            .AddGraphQL()
            .AddValidationRule<MockRule>(null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AllowIntrospection_IntegrationTest_NotAllowed()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(false)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AllowIntrospection_IntegrationTest_Allowed()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(true)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AllowIntrospection_IntegrationTest_NotAllowed_CustomMessage()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(false)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage("Bar")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed_CustomMessageFact()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage(() => "Bar")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed_CustomMessage()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage("Baz")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_Allowed()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
                .AddIntrospectionAllowedRule()
                .BuildRequestExecutorAsync();

        var results = new List<string>();

        var result =
            await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .AllowIntrospection()
                    .Build());
        results.Add(result.ToJson());

        result =
            await executor.ExecuteAsync(
                OperationRequestBuilder
                    .Create()
                    .SetDocument("{ __schema { description } }")
                    .Build());
        results.Add(result.ToJson());

        results.MatchSnapshot();
    }

    [Fact]
    public void SetMaxAllowedValidationErrors_Builder_Is_Null()
    {
        void Fail()
            => RequestExecutorBuilderExtensions.SetMaxAllowedValidationErrors(null!, 6);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    public class MockVisitor : DocumentValidatorVisitor;

    public class MockRule : IDocumentValidatorRule
    {
        public bool IsCacheable => true;

        public void Validate(IDocumentValidatorContext context, DocumentNode document)
        {
            throw new NotImplementedException();
        }
    }
}
