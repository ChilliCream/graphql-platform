using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using Newtonsoft.Json;
using Xunit;
using Zeus.Execution;
using Zeus.Resolvers;

namespace Zeus.Tests
{
    /*
    public class DocumentTests
    {
        [Fact]
        public async Task ExecuteSimpleQuery()
        {
            // arrange
            string schemaDefinition = "type Foo { c : [Bar] } type Bar { d : String } type Query { b : Foo }";
            string queryDefinition = "query a { b { c { d } } }";

            ISchema schema = Schema.Create(schemaDefinition,
                b => b.Add("Query", "b", () => "b")
                    .Add("Foo", "c", () => "c")
                    .Add("Bar", "d", () => "d"));
            Document document = Document.Parse(queryDefinition);

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            IDictionary<string, object> response = await documentExecuter.ExecuteAsync(schema, document, null, null, null, CancellationToken.None);
            string serializedResponse = JsonConvert.SerializeObject(response);

            // assert
            Assert.Equal("{\"b\":{\"c\":[{\"d\":\"d\"}]}}", serializedResponse);
        }

        [Fact]
        public async Task ExecuteSimpleQueryWithDefaultFieldResolver()
        {
            // arrange
            string schemaDefinition = "type Foo { c : [Bar] } type Bar { d : String } type Query { b : Foo }";
            string queryDefinition = "query a { b { c { d } } }";

            ISchema schema = Schema.Create(schemaDefinition,
                b => b.Add("Query", "b", () => "b")
                    .Add<Foo>(t => t.C, c => new Bar()));
            Document document = Document.Parse(queryDefinition);

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            IDictionary<string, object> response = await documentExecuter.ExecuteAsync(schema, document, null, null, null, CancellationToken.None);
            string serializedResponse = JsonConvert.SerializeObject(response);

            // assert
            Assert.Equal("{\"b\":{\"c\":[{\"d\":\"Hello World\"}]}}", serializedResponse);
        }
    }

    public class Foo
    {
        public Bar[] C { get; set; }
    }

    public class Bar
    {
        public string D { get; set; } = "Hello World";
    }
     */
}