using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Validation;
using Xunit;
using HotChocolate.Types;
using Snapshooter.Xunit;

namespace HotChocolate.DependencyInjection
{
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
                    QueryRequestBuilder
                        .New()
                        .SetQuery("{ __schema { description } }")
                        .Create())
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
                    QueryRequestBuilder
                        .New()
                        .SetQuery("{ __schema { description } }")
                        .SetIntrospectionNotAllowedMessage(() => "Bar")
                        .Create())
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
                    QueryRequestBuilder
                        .New()
                        .SetQuery("{ __schema { description } }")
                        .SetIntrospectionNotAllowedMessage("Baz")
                        .Create())
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task AddIntrospectionAllowedRule_IntegrationTest_Allowed()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
                .AddIntrospectionAllowedRule()
                .ExecuteRequestAsync(
                    QueryRequestBuilder
                        .New()
                        .SetQuery("{ __schema { description } }")
                        .AllowIntrospection()
                        .Create())
                .MatchSnapshotAsync();
        }

        public class MockVisitor : DocumentValidatorVisitor
        {
        }

        public class MockRule : IDocumentValidatorRule
        {
            public void Validate(IDocumentValidatorContext context, DocumentNode document)
            {
                throw new NotImplementedException();
            }
        }
    }
}
