using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Language;
using HotChocolate.Utilities;
using ChilliCream.Testing;
using IOPath = System.IO.Path;
using HotChocolate.Stitching.Merge;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching
{
    public class StitchingBuilderTests
        : IClassFixture<TestServerFactory>
    {
        public StitchingBuilderTests(
            TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        public TestServerFactory TestServerFactory { get; }

        [Fact]
        public void AddSchema()
        {
            // arrange
            Schema customerSchema = Schema.Create(
                CustomerSchemaFactory.ConfigureSchema);

            Schema contractSchema = Schema.Create(
                ContractSchemaFactory.ConfigureSchema);

            var builder = new MockStitchingBuilder();

            // act
            builder.AddSchema("customer", customerSchema)
                .AddSchema("contract", contractSchema);

            // assert
            var services = new EmptyServiceProvider();
            var merger = new SchemaMerger();

            foreach (KeyValuePair<NameString, ExecutorFactory> item in
                builder.Executors)
            {
                ISchema schema = item.Value.Invoke(services).Schema;
                merger.AddSchema(item.Key,
                    SchemaSerializer.SerializeSchema(schema));
            }

            SchemaSyntaxSerializer.Serialize(merger.Merge()).MatchSnapshot();
        }

        [Fact]
        public void AddSchema_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            Schema customerSchema = Schema.Create(
                CustomerSchemaFactory.ConfigureSchema);

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchema(null, "foo", customerSchema);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            Schema customerSchema = Schema.Create(
                CustomerSchemaFactory.ConfigureSchema);
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchema(builder, "foo", null);

            // assert
            Assert.Equal("schema",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_SchemaNameIsEmpty_ArgumentNullException()
        {
            // arrange
            Schema customerSchema = Schema.Create(
                CustomerSchemaFactory.ConfigureSchema);
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchema(builder, new NameString(), customerSchema);

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddSchemaFromHttp()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();
            var builder = new MockStitchingBuilder();

            // act
            builder.AddSchemaFromHttp("customer")
                .AddSchemaFromHttp("contract");

            // assert
            var services = new DictionaryServiceProvider(
                typeof(IHttpClientFactory),
                clientFactory);
            var merger = new SchemaMerger();

            foreach (KeyValuePair<NameString, LoadSchemaDocument> item in
                builder.Schemas)
            {
                merger.AddSchema(item.Key, item.Value.Invoke(services));
            }

            SchemaSyntaxSerializer.Serialize(merger.Merge()).MatchSnapshot();
        }

        [Fact]
        public void AddSchemaFromHttp_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromHttp(null, "foo");

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddSchemaFromFile()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();
            var builder = new MockStitchingBuilder();

            // act
            builder.AddSchemaFromFile("contract",
                    IOPath.Combine("__resources__", "Contract.graphql"))
                .AddSchemaFromFile("customer",
                    IOPath.Combine("__resources__", "Customer.graphql"));

            // assert
            var services = new DictionaryServiceProvider(
                typeof(IHttpClientFactory),
                clientFactory);
            var merger = new SchemaMerger();

            foreach (KeyValuePair<NameString, LoadSchemaDocument> item in
                builder.Schemas)
            {
                merger.AddSchema(item.Key, item.Value.Invoke(services));
            }

            SchemaSyntaxSerializer.Serialize(merger.Merge()).MatchSnapshot();
        }

        private IHttpClientFactory CreateRemoteSchemas()
        {
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureSchema,
                ContractSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureSchema,
                CustomerSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(new Func<string, HttpClient>(n =>
                {
                    return n.Equals("contract")
                        ? server_contracts.CreateClient()
                        : server_customers.CreateClient();
                }));
            return httpClientFactory.Object;
        }

        private class MockStitchingBuilder
            : IStitchingBuilder
        {
            public IDictionary<NameString, LoadSchemaDocument> Schemas
            { get; } = new OrderedDictionary<NameString, LoadSchemaDocument>();

            public IDictionary<NameString, ExecutorFactory> Executors
            { get; } = new OrderedDictionary<NameString, ExecutorFactory>();

            public IStitchingBuilder AddExecutionConfiguration(
                Action<IQueryExecutionBuilder> configure)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddExtensions(
                LoadSchemaDocument loadExtensions)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddMergeRule(
                MergeTypeRuleFactory handler)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddTypeRewriter(
                ITypeRewriter rewriter)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddDocumentRewriter(
                IDocumentRewriter rewriter)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddSchema(
                NameString name,
                LoadSchemaDocument loadSchema)
            {
                Schemas[name] = loadSchema;
                return this;
            }

            public IStitchingBuilder AddQueryExecutor(
                NameString name, ExecutorFactory factory)
            {
                Executors[name] = factory;
                return this;
            }

            public IStitchingBuilder AddSchemaConfiguration(
                Action<ISchemaConfiguration> configure)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder SetExecutionOptions(
                IQueryExecutionOptionsAccessor options)
            {
                throw new NotSupportedException();
            }
        }
    }
}
