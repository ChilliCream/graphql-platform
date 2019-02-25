using System;
using System.Collections.Generic;
using System.Net.Http;
using ChilliCream.Testing;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Snapshooter.Xunit;
using Xunit;
using IOPath = System.IO.Path;

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
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchemaFromHttp_NameIsEmpty_ArgumentNullException()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromHttp(builder, new NameString());

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
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

        [Fact]
        public void AddSchemaFromFile_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromFile(null, "foo", "bar");

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchemaFromFile_NameIsEmpty_ArgumentNullException()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromFile(builder, new NameString(), "bar");

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void AddSchemaFromFile_SchemaIsNull_ArgumentNullException(
            string filePath)
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromFile(builder, new NameString(), filePath);

            // assert
            Assert.Equal("path",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddSchemaFromString()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            builder.AddSchemaFromString("contract",
                    FileResource.Open("Contract.graphql"))
                .AddSchemaFromString("customer",
                    FileResource.Open("Customer.graphql"));

            // assert
            var services = new EmptyServiceProvider();
            var merger = new SchemaMerger();

            foreach (KeyValuePair<NameString, LoadSchemaDocument> item in
                builder.Schemas)
            {
                merger.AddSchema(item.Key, item.Value.Invoke(services));
            }

            SchemaSyntaxSerializer.Serialize(merger.Merge()).MatchSnapshot();
        }

        [Fact]
        public void AddSchemaFromString_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromString(null, "foo", "bar");

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchemaFromString_NameIsEmpty_ArgumentNullException()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromString(builder, new NameString(), "bar");

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void AddSchemaFromString_SchemaIsNull_ArgumentNullException(
            string filePath)
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchemaFromString(builder, new NameString(), filePath);

            // assert
            Assert.Equal("schema",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddExtensionsFromString()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            builder.AddExtensionsFromString(
                    FileResource.Open("Contract.graphql"))
                .AddExtensionsFromString(
                    FileResource.Open("Customer.graphql"));

            // assert
            var services = new EmptyServiceProvider();
            var merger = new SchemaMerger();

            var list = new List<string>();

            foreach (LoadSchemaDocument item in builder.Extensions)
            {
                list.Add(SchemaSyntaxSerializer.Serialize(
                    item.Invoke(services)));
            }

            list.MatchSnapshot();
        }

        [Fact]
        public void AddExtensionsFromString_BuilderIsNull_ArgNullException()
        {
            // arrange
            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddExtensionsFromString(null, "foo");

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void AddExtensionsFromString_SchemaIsNull_ArgumentNullException(
            string schema)
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddExtensionsFromString(builder, schema);

            // assert
            Assert.Equal("extensions",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddExtensionsFromFile()
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            builder.AddExtensionsFromFile(
                    IOPath.Combine("__resources__", "Contract.graphql"))
                .AddExtensionsFromFile(
                    IOPath.Combine("__resources__", "Customer.graphql"));

            // assert
            var services = new EmptyServiceProvider();
            var merger = new SchemaMerger();

            var list = new List<string>();

            foreach (LoadSchemaDocument item in builder.Extensions)
            {
                list.Add(SchemaSyntaxSerializer.Serialize(
                    item.Invoke(services)));
            }

            list.MatchSnapshot();
        }

        [Fact]
        public void AddExtensionsFromFile_BuilderIsNull_ArgNullException()
        {
            // arrange
            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddExtensionsFromFile(null, "foo");

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void AddExtensionsFromFile_SchemaIsNull_ArgumentNullException(
            string filePath)
        {
            // arrange
            var builder = new MockStitchingBuilder();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddExtensionsFromFile(builder, filePath);

            // assert
            Assert.Equal("path",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddTypeRewriter()
        {
            // arrange
            ITypeRewriter typeRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddTypeRewriter(It.IsAny<ITypeRewriter>()))
                .Returns(new Func<ITypeRewriter, IStitchingBuilder>(t =>
                {
                    typeRewriter = t;
                    return mock.Object;
                }));

            // act
            StitchingBuilderExtensions
                .AddTypeRewriter(mock.Object, (schema, type) => type);

            // assert
            Assert.IsType<DelegateTypeRewriter>(typeRewriter);
        }

        [Fact]
        public void AddTypeRewriter_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => StitchingBuilderExtensions
                .AddTypeRewriter(null, (schema, type) => type);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddTypeRewriter_DelegateIsNull_ArgumentNullException()
        {
            // arrange
            ITypeRewriter typeRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddTypeRewriter(It.IsAny<ITypeRewriter>()))
                .Returns(new Func<ITypeRewriter, IStitchingBuilder>(t =>
                {
                    typeRewriter = t;
                    return mock.Object;
                }));

            // act
            Action action = () => StitchingBuilderExtensions
                .AddTypeRewriter(mock.Object,
                    (RewriteTypeDefinitionDelegate)null);

            // assert
            Assert.Equal("rewrite",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddDocumentRewriter()
        {
            // arrange
            IDocumentRewriter typeRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    typeRewriter = t;
                    return mock.Object;
                }));

            // act
            StitchingBuilderExtensions
                .AddDocumentRewriter(mock.Object, (schema, doc) => doc);

            // assert
            Assert.IsType<DelegateDocumentRewriter>(typeRewriter);
        }

        [Fact]
        public void AddDocumentRewriter_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => StitchingBuilderExtensions
                .AddDocumentRewriter(null, (schema, doc) => doc);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddDocumentRewriter_DelegateIsNull_ArgumentNullException()
        {
            // arrange
            IDocumentRewriter typeRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    typeRewriter = t;
                    return mock.Object;
                }));

            // act
            Action action = () => StitchingBuilderExtensions
                .AddDocumentRewriter(mock.Object,
                    (RewriteDocumentDelegate)null);

            // assert
            Assert.Equal("rewrite",
                Assert.Throws<ArgumentNullException>(action).ParamName);
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
            public IList<LoadSchemaDocument> Extensions { get; } =
                new List<LoadSchemaDocument>();

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
                Extensions.Add(loadExtensions);
                return this;
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
