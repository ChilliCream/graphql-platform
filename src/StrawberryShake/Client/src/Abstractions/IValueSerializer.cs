using System;
using System.Collections.Generic;
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
        private readonly IOperationExecutor<GetFooResult> _operationExecutor;
        private readonly IOperationStore _operationStore;

        public GetFooQuery(
            IOperationExecutor<GetFooResult> operationExecutor,
            IOperationStore operationStore)
        {
            _operationExecutor = operationExecutor;
            _operationStore = operationStore;
        }

        public async Task<IOperationResult<GetFooResult>> ExecuteAsync(
            string a, string b, string c,
            CancellationToken cancellationToken = default)
        {
            var request = new GetFooQueryRequest();
            request.Variables.Add("a", a);
            request.Variables.Add("b", b);
            request.Variables.Add("c", c);

            return await _operationExecutor
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        public IOperationObservable<GetFooResult> Watch(
            string a, string b, string c)
        {
            throw new NotImplementedException();
        }


        private class GetFooQueryObservable : IOperationObservable<GetFooResult>
        {
            private readonly IOperationExecutor<GetFooResult> _operationExecutor;
            private readonly IOperationStore _operationStore;
            private readonly GetFooQueryRequest _request;

            public IDisposable Subscribe(
                IObserver<IOperationResult<GetFooResult>> observer)
            {
                throw new NotImplementedException();
            }

            public ValueTask<IAsyncDisposable> SubscribeAsync(
                IObserver<string> observer,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(
                Action<IOperationResult<GetFooResult>> next,
                CancellationToken cancellationToken = default)
            {



                throw new NotImplementedException();
            }

            public void Subscribe(
                Func<IOperationResult<GetFooResult>, ValueTask> nextAsync,
                CancellationToken cancellationToken = default)
            {
                Task.Run(async () =>
                    {
                        _operationStore
                            .Watch<GetFooResult>(_request)
                            .Subscribe(nextAsync, cancellationToken);

                        await _operationExecutor
                            .ExecuteAsync(_request, cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken);
            }
        }
    }


    public class GetFooQueryRequest : IOperationRequest
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
