using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

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
    [Obsolete]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AllowIntrospection_IntegrationTest_NotAllowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(false)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AllowIntrospection_IntegrationTest_Allowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(true)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AllowIntrospection_IntegrationTest_NotAllowed_CustomMessage()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AllowIntrospection(false)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage("Bar")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed_CustomMessageFact()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage(() => "Bar")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_NotAllowed_CustomMessage()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddIntrospectionAllowedRule()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage("Baz")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    [Obsolete]
    public async Task AddIntrospectionAllowedRule_IntegrationTest_Allowed()
    {
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
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .AllowIntrospection()
                    .Build());
        results.Add(result.ToJson());

        result =
            await executor.ExecuteAsync(
                OperationRequestBuilder
                    .New()
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
        public ushort Priority => ushort.MaxValue;
        public bool IsCacheable => true;

        public void Validate(IDocumentValidatorContext context, DocumentNode document)
        {
            throw new NotImplementedException();
        }
    }
}
