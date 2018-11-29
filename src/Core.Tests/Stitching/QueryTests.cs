
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching
{
    public class QueryTests
    {
        public void Test()
        {
            // arrange
            string schema_a = @"
                type Query { foo: Foo }
                type Foo { name: String }";

            string schema_b = @"
                type Query { bar: Bar }
                type Bar { name: String }";

            string schema_stiched = @"
                type Query { foo: Foo @schema(name: ""a"") }
                type Foo {
                    name: String @schema(name: ""a"")
                    bar: Bar @schema(name: ""b"") @delegate(path: """") }";

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

            ISchema schema = Schema.Create(schema_stiched, c =>
            {

            });




        }


    }
}
