using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class StreamHelper
{
    private delegate IAsyncEnumerable<object?> Factory(object result);

    private static readonly MethodInfo _createStreamFromAsyncEnumerable =
        typeof(StreamHelper).GetMethod(
            nameof(CreateStreamFromAsyncEnumerable),
            BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo _createStreamFromEnumerable =
        typeof(StreamHelper).GetMethod(
            nameof(CreateStreamFromEnumerable),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, Factory> _streamFactories = new();

    public static IAsyncEnumerable<object?> CreateStream(object result)
    {
        var resultType = result.GetType();
        var factory = _streamFactories.GetOrAdd(resultType, t => CreateFactory(t));
        return factory.Invoke(result);
    }

    private static Factory CreateFactory(Type resultType)
    {
        var resultTypeInfo = CreateResultTypeInfo(resultType);
        var resultParameter = Expression.Parameter(typeof(object), "result");
        var method = resultTypeInfo.IsAsyncEnumerable
            ? _createStreamFromAsyncEnumerable.MakeGenericMethod(resultTypeInfo.ElementType)
            : _createStreamFromEnumerable.MakeGenericMethod(resultTypeInfo.ElementType);
        var castResult = Expression.Convert(resultParameter, resultType);
        var callMethod = Expression.Call(method, castResult);
        return Expression.Lambda<Factory>(callMethod, resultParameter).Compile();
    }

    private static ResultTypeInfo CreateResultTypeInfo(Type resultType)
    {
        var interfaces = resultType.GetInterfaces();
        Type? elementType = null;

        for (var index = 0; index < interfaces.Length; index++)
        {
            var interfaceType = interfaces[index];

            if (interfaceType.IsGenericType)
            {
                var arguments = interfaceType.GetGenericArguments();

                if (arguments.Length == 1)
                {
                    var typeDefinition = interfaceType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return new ResultTypeInfo(arguments[0], true);
                    }

                    if(elementType is null && typeDefinition == typeof(IEnumerable<>))
                    {
                        elementType = arguments[0];
                    }
                }
            }
        }

        if (elementType is not null)
        {
            return new ResultTypeInfo(elementType, false);
        }

        // TODO : EXCEPTION
        throw new GraphQLException("The result type is not streamable!");
    }

    private static IAsyncEnumerable<object?> CreateStreamFromAsyncEnumerable<T>(
        IAsyncEnumerable<T> asyncEnumerable)
        => new AsyncEnumerableFromAsyncEnumerable<T>(asyncEnumerable);

    private static IAsyncEnumerable<object?> CreateStreamFromEnumerable<T>(
        IEnumerable<T> enumerable)
        => enumerable is IAsyncEnumerable<T> asyncEnumerable
            ? CreateStreamFromAsyncEnumerable(asyncEnumerable)
            : new AsyncEnumerableFromEnumerable<T>(enumerable);

    private readonly ref struct ResultTypeInfo
    {
        public ResultTypeInfo(Type elementType, bool isAsyncEnumerable)
        {
            IsAsyncEnumerable = isAsyncEnumerable;
            ElementType = elementType;
        }

        public Type ElementType { get; }

        public bool IsAsyncEnumerable { get; }
    }

    private sealed class AsyncEnumerableFromEnumerable<T> : IAsyncEnumerable<object?>
    {
        private readonly IEnumerable<T> _enumerable;

        public AsyncEnumerableFromEnumerable(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        public IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
            => new AsyncEnumerableFromEnumerator(_enumerable, cancellationToken);

        private sealed class AsyncEnumerableFromEnumerator : IAsyncEnumerator<object?>
        {
            private readonly IEnumerable<T> _enumerator;
            private readonly CancellationToken _cancellationToken;
            private List<T>? _list;
            private int _index;

            public AsyncEnumerableFromEnumerator(
                IEnumerable<T> enumerator,
                CancellationToken cancellationToken)
            {
                _enumerator = enumerator;
                _cancellationToken = cancellationToken;
            }

            public object? Current { get; private set; }

            public async ValueTask<bool> MoveNextAsync()
            {
                _list ??= await Task.Factory.StartNew(
                    () => _enumerator.ToList(),
                    _cancellationToken,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);

                if (_index >= _list.Count)
                {
                    return false;
                }

                Current = _list[_index++];
                return true;
            }

            public ValueTask DisposeAsync() => default;
        }
    }

    private sealed class AsyncEnumerableFromAsyncEnumerable<T> : IAsyncEnumerable<object?>
    {
        private readonly IAsyncEnumerable<T> _enumerable;

        public AsyncEnumerableFromAsyncEnumerable(IAsyncEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        public async IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await foreach (var element in
                _enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return element;
            }
        }
    }
}
