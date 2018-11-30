
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

            schemas["a"] = new QueryExecuter(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = new QueryExecuter(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var stitchingContext = new StitchingContext(schemas);
            var broker = new QueryBroker(stitchingContext);

            var directive = new Mock<IDirective>();
            directive.Setup(t => t.ToObject<DelegateDirective>())
                .Returns(new DelegateDirective());

            var directiveContext = new Mock<IDirectiveContext>();
            directiveContext.SetupGet(t => t.FieldSelection)
                .Returns(fieldSelection);
            directiveContext.SetupGet(t => t.Directive)
                .Returns(directive.Object);

            // act
            IExecutionResult response = await broker.RedirectQueryAsync(
                directiveContext.Object);

            // assert
            response.Snapshot();
        }


        [Fact]
        public async Task Foo()
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

            schemas["a"] = new QueryExecuter(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = new QueryExecuter(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var stitchingContext = new StitchingContext(schemas);
            var broker = new QueryBroker(stitchingContext);

            var services = new ServiceCollection();
            services.AddSingleton<IStitchingContext>(stitchingContext);
            services.AddSingleton<IQueryBroker, QueryBroker>();
            services.AddSingleton<IQueryParser, AnnotationQueryParser>();
            services.AddSingleton<ISchema>(sp => Schema.Create(
                schema_stiched,
                c =>
                {
                    c.RegisterServiceProvider(sp);
                    c.Use(next => context =>
                    {
                        switch (context.Parent<object>())
                        {
                            case IDictionary<string, object> dict:
                                context.Result = dict[next.]

                        }
                        return next(context);
                    });
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<SchemaDirectiveType>();
                }));

            ISchema schema = services.BuildServiceProvider().GetService<ISchema>();

            IExecutionResult result = await schema.ExecuteAsync(query);

            result.Snapshot();




        }
    }
}
