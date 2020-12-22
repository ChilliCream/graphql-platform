using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using StrawberryShake.Remove;

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

    public class Usage
    {
        public void Foo(GetFooQuery query)
        {
            // query.ExecuteAsync()

            query
                .Watch("a")
                .Subscribe(result =>
                {
                    Console.WriteLine(result.Data!.Foo.Bar);
                });
        }
    }
}
