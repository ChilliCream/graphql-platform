using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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


    public class GetFooQuery
    {
        public Task<IOperationResult<GetFooResult>> ExecuteAsync(
            string a, string b, string c,
            CancellationToken cancellationToken = default)
        {
            // execute pipeline
            // network only
            // request serialize => Send => Receive => (IResultReader, Store) => Result
            // store first
            // TryGetFromStore => network only
            // store and network
            // TryGetFromStore => always network only
            throw new NotImplementedException();
        }

        public IOperationObservable<GetFooResult> Watch(
            string a, string b, string c)
        {
            throw new NotImplementedException();
        }
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

    public class GetFooResult
    {
        public Foo Foo { get; }
    }


    public partial class Foo
    {
        public string Bar { get; }

        public Baz Baz { get;  }
    }
}
