using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;

namespace StrawberryShake
{

    // does just know entities and data
    public interface IEntityWriter<in TData, in TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        string Name { get; }

        EntityId ExtractId(TData data);

        void Write(TData data, TEntity entity);
    }


    // knows about operation store and entity store and data
    public interface IResultReader
    {
        //OperationResult<FooQueryResult> Parse(Stream stream);
    }


    public class GetFooQueryRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IDocument Document { get; set; }
        public IDictionary<string, object> Variables { get; }
        public IDictionary<string, object> Extensions { get; }
        public IDictionary<string, object> ContextData { get; }
    }

    public static class Usage
    {
        public static void TestMe(GetFooQuery query)
        {
            query
                .Watch("a", "a", "a")
                .Subscribe(result =>
                {
                    Console.WriteLine(result.Data?.Foo.Bar);
                });
        }
    }

    /*
     * query GetFoo {
     * foo {
     *   bar
     * }
     * }
     */
}
