
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class QueryBrokerTests
    {
        [Fact]
        public async Task ExtractRemoteQueryFromField()
        {
            // arrange
            string schema_a = @"
                type Query { foo: Foo }
                type Foo { name: String }";

            string schema_b = @"
                type Query { bar: Bar }
                type Bar { name: String }";

            string query = @"
                {
                    foo
                        @schema(name: ""a"")
                        @delegate(path: ""foo"" operation: QUERY)
                    {
                        name @schema(name: ""a"")
                        bar
                            @schema(name: ""b"")
                            @delegate(path: ""bar"" operation: QUERY)
                        {
                            name @schema(name: ""b"")
                        }
                    }
                }";

            DocumentNode queryDocument = Parser.Default.Parse(query);
            FieldNode fieldSelection = queryDocument.Definitions
                .OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().Last();

            var schemas = new Dictionary<string, IQueryExecuter>();

            schemas["a"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var stitchingContext = new StitchingContext(schemas);
            var broker = new QueryBroker();

            var directive = new Mock<IDirective>();
            directive.Setup(t => t.ToObject<DelegateDirective>())
                .Returns(new DelegateDirective());

            var directiveContext = new Mock<IDirectiveContext>();
            directiveContext.SetupGet(t => t.FieldSelection)
                .Returns(fieldSelection);
            directiveContext.SetupGet(t => t.Directive)
                .Returns(directive.Object);
            directiveContext.Setup(t => t.Service<IStitchingContext>())
                .Returns(stitchingContext);

            // act
            IExecutionResult response = await broker.RedirectQueryAsync(
                directiveContext.Object);

            // assert
            response.Snapshot();
        }

        [Fact]
        public async Task ExecuteQueryOnStitchedSchema()
        {
            // arrange
            string schema_a = @"
                type Query { foo: Foo }
                type Foo { name: String }";

            string schema_b = @"
                type Query { bar: Bar }
                type Bar { name: String }";

            string schema_stiched = @"
                type Query {
                    foo: Foo
                        @schema(name: ""a"")
                        @delegate
                }
                type Foo {
                    name: String @schema(name: ""a"")
                    bar: Bar
                        @schema(name: ""b"")
                        @delegate
                }
                type Bar {
                    name: String @schema(name: ""b"")
                }";

            string query = @"
                {
                    foo
                    {
                        name
                        bar
                        {
                            name
                        }
                    }
                }";

            DocumentNode queryDocument = Parser.Default.Parse(query);
            FieldNode fieldSelection = queryDocument.Definitions
                .OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().Last();

            var schemas = new Dictionary<string, IQueryExecuter>();

            schemas["a"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var services = new ServiceCollection();
            services.AddSingleton<IStitchingContext>(
                new StitchingContext(schemas));
            services.AddSingleton<IQueryBroker, QueryBroker>();
            services.AddSingleton<IQueryParser, AnnotationQueryParser>();
            services.AddSingleton<ISchema>(sp => Schema.Create(
                schema_stiched,
                c =>
                {
                    c.RegisterServiceProvider(sp);
                    c.UseStitching();
                }));

            var schema = services.BuildServiceProvider().GetService<ISchema>();

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            result.Snapshot();
        }


        [Fact(Skip = "Not Finalized")]
        public async Task ExecuteQueryOnStitchedSchema1()
        {
            // arrange
            string schema_a = @"
                type Query { item: Item }
                type Foo implements Item { name1: String name2: String }
                interface Item { name1: String }";

            string schema_b = @"
                type Query { bar: Bar }
                type Bar { name: String }";

            string schema_stiched = @"
                type Query {
                    item: Item
                        @schema(name: ""a"")
                        @delegate
                }
                interface Item { name1: String }
                type Foo implements Item {
                    name1: String @schema(name: ""a"")
                    name2: String @schema(name: ""a"")
                    bar: Bar
                        @schema(name: ""b"")
                        @delegate
                }
                type Bar {
                    name: String @schema(name: ""b"")
                }";

            string query = @"
                {
                    item
                    {
                        ... on Foo {
                            name1
                            name2
                            bar
                            {
                                name
                            }
                        }
                    }
                }";

            DocumentNode queryDocument = Parser.Default.Parse(query);
            FieldNode fieldSelection = queryDocument.Definitions
                .OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().Last();

            var schemas = new Dictionary<string, IQueryExecuter>();

            schemas["a"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = QueryExecutionBuilder.BuildDefault(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var services = new ServiceCollection();
            services.AddSingleton<IStitchingContext>(
                new StitchingContext(schemas));
            services.AddSingleton<IQueryBroker, QueryBroker>();
            services.AddSingleton<IQueryParser, AnnotationQueryParser>();
            services.AddSingleton<ISchema>(sp => Schema.Create(
                schema_stiched,
                c =>
                {
                    c.RegisterServiceProvider(sp);
                    c.UseStitching();
                }));

            var schema = services.BuildServiceProvider().GetService<ISchema>();

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            result.Snapshot();
        }
    }
}
