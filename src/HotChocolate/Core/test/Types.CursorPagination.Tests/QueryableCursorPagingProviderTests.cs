using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable  enable

namespace HotChocolate.Types.Pagination;

public class QueryableCursorPagingProviderTests
{
    [Fact]
    public async Task TakeFirst()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(2);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("a", t.Node);
                Assert.Equal(0, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("b", t.Node);
                Assert.Equal(1, GetPositionFromCursor(t.Cursor));
            });

        Assert.False(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.True(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task TakeLastSingle()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "f", "g", };

        var pagingDetails = new CursorPagingArguments(last: 1);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("g", t.Node);
                Assert.Equal(1, GetPositionFromCursor(t.Cursor));
            });

        Assert.True(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.False(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task TakeLast()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(last: 2);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("f", t.Node);
                Assert.Equal(5, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("g", t.Node);
                Assert.Equal(6, GetPositionFromCursor(t.Cursor));
            });

        Assert.True(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.False(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task TakeFirstAfter()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments();
        var context = new MockContext(pagingDetails);
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        pagingDetails = new CursorPagingArguments(after: connection.Info.StartCursor, first: 2);
        context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("b", t.Node);
                Assert.Equal(1, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("c", t.Node);
                Assert.Equal(2, GetPositionFromCursor(t.Cursor));
            });

        Assert.True(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.True(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task TakeLastBefore()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(first: 5);
        var context = new MockContext(pagingDetails);
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        pagingDetails = new CursorPagingArguments(before: connection.Info.EndCursor, last: 2);
        context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("c", t.Node);
                Assert.Equal(2, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("d", t.Node);
                Assert.Equal(3, GetPositionFromCursor(t.Cursor));
            });

        Assert.True(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.True(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task HasNextPage_True()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(first: 5);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.True(connection.Info.HasNextPage);
    }

    [Fact]
    public async Task HasNextPage_False()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(first: 7);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.False(connection.Info.HasNextPage);
    }

    [Fact]
    public async Task HasPrevious_True()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(first: 1);
        var context = new MockContext(pagingDetails);

        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        pagingDetails = new CursorPagingArguments(after: connection.Info.EndCursor, first: 2);
        context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.True(connection.Info.HasPreviousPage);
    }

    [Fact]
    public async Task HasPrevious_False()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

        var pagingDetails = new CursorPagingArguments(first: 1);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.False(connection.Info.HasPreviousPage);
    }

    [Fact]
    public async Task Executable()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new MockExecutable<string>(new []
        {
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
        });

        var pagingDetails = new CursorPagingArguments(2);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("a", t.Node);
                Assert.Equal(0, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("b", t.Node);
                Assert.Equal(1, GetPositionFromCursor(t.Cursor));
            });

        Assert.False(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.True(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    [Fact]
    public async Task Executable_Queryable()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var sourceType = typeInspector.GetType(typeof(List<string>));

        IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
        var pagingHandler = pagingProvider.CreateHandler(sourceType, new());

        var list = new MockExecutable<string>(new []
        {
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
        }.AsQueryable());

        var pagingDetails = new CursorPagingArguments(2);
        var context = new MockContext(pagingDetails);

        // act
        pagingHandler.PublishPagingArguments(context);
        var connection = (Connection)await pagingHandler.SliceAsync(context, list);

        // assert
        Assert.Collection(connection.Edges,
            t =>
            {
                Assert.Equal("a", t.Node);
                Assert.Equal(0, GetPositionFromCursor(t.Cursor));
            },
            t =>
            {
                Assert.Equal("b", t.Node);
                Assert.Equal(1, GetPositionFromCursor(t.Cursor));
            });

        Assert.False(
            connection.Info.HasPreviousPage,
            "HasPreviousPage");

        Assert.True(
            connection.Info.HasNextPage,
            "HasNextPage");
    }

    private int GetPositionFromCursor(string cursor)
    {
        return int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
    }

    private sealed class MockContext : IResolverContext
    {
        private readonly CursorPagingArguments _arguments;

        public MockContext(CursorPagingArguments arguments)
        {
            _arguments = arguments;
        }

        public IServiceProvider Services
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public IServiceProvider RequestServices => throw new NotImplementedException();

        public string ResponseName => throw new NotImplementedException();

        public bool HasErrors => throw new NotImplementedException();

        public IImmutableDictionary<string, object?> ScopedContextData
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public IImmutableDictionary<string, object?> LocalContextData { get; set; } =
            ImmutableDictionary<string, object?>.Empty;

        public CancellationToken RequestAborted => default;

        public InputParser Parser => throw new NotImplementedException();

        public ITypeConverter Converter => throw new NotImplementedException();

        public object Service(Type service)
        {
            throw new NotImplementedException();
        }

        public void ReportError(string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void ReportError(IError error)
        {
            throw new NotImplementedException();
        }

        public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<ISelection> GetSelections(
            IObjectType typeContext,
            ISelection? selection = null,
            bool allowInternals = false)
        {
            throw new NotImplementedException();
        }

        public ISelectionCollection Select()
        {
            throw new NotImplementedException();
        }

        public ISelectionCollection Select(string fieldName)
        {
            throw new NotImplementedException();
        }

        public T GetQueryRoot<T>()
        {
            throw new NotImplementedException();
        }

        public IResolverContext Clone()
        {
            throw new NotImplementedException();
        }

        public ISchema Schema => throw new NotImplementedException();

        public IObjectType ObjectType => throw new NotImplementedException();

        public IOperation Operation => throw new NotImplementedException();

        public ISelection Selection => throw new NotImplementedException();

        public IVariableValueCollection Variables => throw new NotImplementedException();

        public Path Path => throw new NotImplementedException();

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }

        public T ArgumentValue<T>(string name)
        {
            switch (name)
            {
                case "first":
                    if (_arguments.First is null)
                    {
                        return default!;
                    }
                    if (_arguments.First is T first)
                    {
                        return first;
                    }
                    break;
                case "last":
                    if (_arguments.Last is null)
                    {
                        return default!;
                    }
                    if (_arguments.Last is T last)
                    {
                        return last;
                    }
                    break;
                case "after":
                    if (_arguments.After is null)
                    {
                        return default!;
                    }
                    if (_arguments.After is T after)
                    {
                        return after;
                    }
                    break;
                case "before":
                    if (_arguments.Before is null)
                    {
                        return default!;
                    }
                    if (_arguments.Before is T before)
                    {
                        return before;
                    }
                    break;
            }

            throw new InvalidOperationException();
        }

        public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
        {
            throw new NotImplementedException();
        }

        public Optional<T> ArgumentOptional<T>(string name)
        {
            throw new NotImplementedException();
        }

        public ValueKind ArgumentKind(string name)
        {
            throw new NotImplementedException();
        }

        public T Service<T>() where T : notnull
        {
            throw new NotImplementedException();
        }

        public T? Service<T>(object key) where T : notnull
        {
            throw new NotImplementedException();
        }

        public T Resolver<T>()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object?> ContextData => throw new NotImplementedException();
    }

    private sealed class MockExecutable<T>(IEnumerable<T> source) : IExecutable<T> where T : class
    {
        public object Source => source;

        ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
            => new(source.ToList());

        public ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
            => new(source.ToList());

        public async IAsyncEnumerable<T> ToAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var queryable = await new ValueTask<IEnumerable<T>>(source);

            foreach (var item in queryable)
            {
                yield return item;
            }
        }

        async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var queryable = await new ValueTask<IEnumerable<T>>(source);

            foreach (var item in queryable)
            {
                yield return item;
            }
        }

        ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.FirstOrDefault());

        public ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.FirstOrDefault());

        ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.SingleOrDefault());

        public ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.SingleOrDefault());

        public string Print() => source.ToString()!;
    }
}
