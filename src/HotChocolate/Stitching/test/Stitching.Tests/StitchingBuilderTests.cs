using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Utilities;
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
            StitchingBuilder stitchingBuilder = StitchingBuilder.New();

            DocumentNode schema_a = Utf8GraphQLParser.Parse(
                "type Query { a: A } type A { b: String }");
            DocumentNode schema_b = Utf8GraphQLParser.Parse(
                "type Query { b: B } type B { c: String }");

            // act
            stitchingBuilder
                .AddSchema("a", s => schema_a)
                .AddSchema("b", s => schema_b);

            // assert
            var services = new ServiceCollection();
            stitchingBuilder.Populate(services);

            DocumentNode schema = services.BuildServiceProvider()
                .GetRequiredService<StitchingBuilder.StitchingFactory>()
                .MergedSchema;

            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddSchema_SchemaAlreadyRegistered_ArgumentNullException()
        {
            // arrange
            StitchingBuilder stitchingBuilder = StitchingBuilder.New();
            NameString schemaName = "abc";

            stitchingBuilder.AddSchema(
                schemaName,
                sp => new DocumentNode(new List<IDefinitionNode>()));

            // act
            Action action = () => stitchingBuilder.AddSchema(
                schemaName,
                sp => new DocumentNode(new List<IDefinitionNode>()));

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_SchemaAlreadyRegistered_2_ArgumentNullException()
        {
            // arrange
            StitchingBuilder stitchingBuilder = StitchingBuilder.New();
            NameString schemaName = "abc";

            stitchingBuilder.AddQueryExecutor(
                schemaName,
                sp => default(IQueryExecutor));

            // act
            Action action = () => stitchingBuilder.AddSchema(
                schemaName,
                sp => new DocumentNode(new List<IDefinitionNode>()));

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_SchemaNameIsEmpty_ArgumentNullException()
        {
            // arrange
            StitchingBuilder stitchingBuilder = StitchingBuilder.New();
            NameString schemaName = "abc";

            // act
            Action action = () => stitchingBuilder.AddSchema(
                null, sp => new DocumentNode(new List<IDefinitionNode>()));

            // assert
            Assert.Equal("name",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_LoadSchemaIsNull_ArgumentNullException()
        {
            // arrange
            StitchingBuilder stitchingBuilder = StitchingBuilder.New();
            NameString schemaName = "abc";

            // act
            Action action = () => stitchingBuilder.AddSchema(
                schemaName, (LoadSchemaDocument)null);

            // assert
            Assert.Equal("loadSchema",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_2()
        {
            // arrange
            ISchema customerSchema = CustomerSchemaFactory.Create();

            ISchema contractSchema = ContractSchemaFactory.Create();

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
        public void AddSchema_2_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            ISchema customerSchema = CustomerSchemaFactory.Create();

            // act
            Action action = () =>
                StitchingBuilderExtensions
                    .AddSchema(null, "foo", customerSchema);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void AddSchema_2_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            ISchema customerSchema = CustomerSchemaFactory.Create();
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
        public void AddSchema_2_SchemaNameIsEmpty_ArgumentNullException()
        {
            // arrange
            ISchema customerSchema = CustomerSchemaFactory.Create();
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
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));

            // act
            StitchingBuilderExtensions
                .AddDocumentRewriter(mock.Object, (schema, doc) => doc);

            // assert
            Assert.IsType<DelegateDocumentRewriter>(docRewriter);
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
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
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

        [Fact]
        public void IgnoreRootTypes()
        {
            // arrange
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));

            // act
            StitchingBuilderExtensions.IgnoreRootTypes(mock.Object);

            // assert
            RemoveRootTypeRewriter rewriter =
                Assert.IsType<RemoveRootTypeRewriter>(docRewriter);
            Assert.Null(rewriter.SchemaName);
        }

        [Fact]
        public void IgnoreRootTypes_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreRootTypes(null);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void IgnoreRootTypes_2()
        {
            // arrange
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));
            NameString schemaName = "Foo";

            // act
            StitchingBuilderExtensions.IgnoreRootTypes(mock.Object, schemaName);

            // assert
            RemoveRootTypeRewriter rewriter =
                Assert.IsType<RemoveRootTypeRewriter>(docRewriter);
            Assert.Equal(schemaName, rewriter.SchemaName);
        }

        [Fact]
        public void IgnoreRootTypes_2_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            NameString schemaName = "Foo";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreRootTypes(null, schemaName);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void IgnoreRootTypes_2_NameIsEmpty_ArgumentException()
        {
            // arrange
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreRootTypes(mock.Object, null);

            // assert
            Assert.Equal("schemaName",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void IgnoreType()
        {
            // arrange
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));
            NameString typeName = "Foo";

            // act
            StitchingBuilderExtensions.IgnoreType(mock.Object, typeName);

            // assert
            RemoveTypeRewriter rewriter =
                Assert.IsType<RemoveTypeRewriter>(docRewriter);
            Assert.Null(rewriter.SchemaName);
            Assert.Equal(typeName, rewriter.TypeName);
        }

        [Fact]
        public void IgnoreType_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            NameString typeName = "Foo";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreType(null, typeName);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void IgnoreType_NameIsEmpty_ArgumentException()
        {
            // arrange
            var mock = new Mock<IStitchingBuilder>();

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreType(mock.Object, null);

            // assert
            Assert.Equal("typeName",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void IgnoreType_2()
        {
            // arrange
            IDocumentRewriter docRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddDocumentRewriter(
                    It.IsAny<IDocumentRewriter>()))
                .Returns(new Func<IDocumentRewriter, IStitchingBuilder>(t =>
                {
                    docRewriter = t;
                    return mock.Object;
                }));
            NameString schemaName = "Foo";
            NameString typeName = "Bar";

            // act
            StitchingBuilderExtensions.IgnoreType(
                mock.Object, schemaName, typeName);

            // assert
            RemoveTypeRewriter rewriter =
                Assert.IsType<RemoveTypeRewriter>(docRewriter);
            Assert.Equal(schemaName, rewriter.SchemaName);
            Assert.Equal(typeName, rewriter.TypeName);
        }

        [Fact]
        public void IgnoreType_2_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            NameString schemaName = "Foo";
            NameString typeName = "Bar";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreType(null, schemaName, typeName);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void IgnoreType_2_SchemaIsEmpty_ArgumentException()
        {
            // arrange
            var mock = new Mock<IStitchingBuilder>();
            NameString typeName = "Bar";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreType(mock.Object, null, typeName);

            // assert
            Assert.Equal("schemaName",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void IgnoreType_2_NameIsEmpty_ArgumentException()
        {
            // arrange
            var mock = new Mock<IStitchingBuilder>();
            NameString schemaName = "Foo";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreType(mock.Object, schemaName, null);

            // assert
            Assert.Equal("typeName",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void IgnoreField()
        {
            // arrange
            ITypeRewriter typeRewriter = null;
            var mock = new Mock<IStitchingBuilder>();
            mock.Setup(t => t.AddTypeRewriter(
                    It.IsAny<ITypeRewriter>()))
                .Returns(new Func<ITypeRewriter, IStitchingBuilder>(t =>
                {
                    typeRewriter = t;
                    return mock.Object;
                }));
            NameString schemaName = "Foo";
            var fieldReference = new FieldReference("A", "a");

            // act
            StitchingBuilderExtensions.IgnoreField(
                mock.Object, schemaName, fieldReference);

            // assert
            RemoveFieldRewriter rewriter =
                Assert.IsType<RemoveFieldRewriter>(typeRewriter);
            Assert.Equal(schemaName, rewriter.SchemaName);
            Assert.Equal(fieldReference.TypeName, rewriter.Field.TypeName);
            Assert.Equal(fieldReference.FieldName, rewriter.Field.FieldName);
        }

        [Fact]
        public void IgnoreField_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            NameString schemaName = "Foo";
            var fieldReference = new FieldReference("A", "a");

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreField(null, schemaName, fieldReference);

            // assert
            Assert.Equal("builder",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        [Fact]
        public void IgnoreField_SchemaIsEmpty_ArgumentNullException()
        {
            // arrange
            var mock = new Mock<IStitchingBuilder>();
            var fieldReference = new FieldReference("A", "a");

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreField(mock.Object, null, fieldReference);

            // assert
            Assert.Equal("schemaName",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void IgnoreField_FieldIsNull_ArgumentNullException()
        {
            // arrange
            var mock = new Mock<IStitchingBuilder>();
            NameString schemaName = "Foo";

            // act
            Action action = () => StitchingBuilderExtensions
                .IgnoreField(mock.Object, schemaName, null);

            // assert
            Assert.Equal("field",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }

        private IHttpClientFactory CreateRemoteSchemas()
        {
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

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

            public IStitchingBuilder AddMergedDocumentRewriter(
                Func<DocumentNode, DocumentNode> rewrite)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddMergedDocumentVisitor(
                Action<DocumentNode> visit)
            {
                throw new NotSupportedException();
            }

            public IStitchingBuilder AddTypeMergeRule(MergeTypeRuleFactory factory)
            {
                throw new NotImplementedException();
            }

            public IStitchingBuilder AddDirectiveMergeRule(MergeDirectiveRuleFactory factory)
            {
                throw new NotImplementedException();
            }

            public IStitchingBuilder SetSchemaCreation(SchemaCreation creation)
            {
                throw new NotImplementedException();
            }
        }
    }
}
