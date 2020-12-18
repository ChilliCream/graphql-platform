using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Xunit;

#nullable  enable

namespace HotChocolate.Types.Pagination
{
    public class QueryableCursorPagingProviderTests
    {
        [Fact]
        public async Task TakeFirst()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(2);
            var context = new MockContext(pagingDetails);

            // act
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
        public async Task TakeLast()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(last: 2);
            var context = new MockContext(pagingDetails);

            // act
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
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments();
            var context = new MockContext(pagingDetails);
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            pagingDetails = new CursorPagingArguments(after: connection.Info.StartCursor, first: 2);
            context = new MockContext(pagingDetails);

            // act
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
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(first: 5);
            var context = new MockContext(pagingDetails);
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            pagingDetails = new CursorPagingArguments(before: connection.Info.EndCursor, last: 2);
            context = new MockContext(pagingDetails);

            // act
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
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(first: 5);
            var context = new MockContext(pagingDetails);

            // act
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            // assert
            Assert.True(connection.Info.HasNextPage);
        }

        [Fact]
        public async Task HasNextPage_False()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(first: 7);
            var context = new MockContext(pagingDetails);

            // act
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            // assert
            Assert.False(connection.Info.HasNextPage);
        }

        [Fact]
        public async Task HasPrevious_True()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(first: 1);
            var context = new MockContext(pagingDetails);
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            pagingDetails = new CursorPagingArguments(after: connection.Info.EndCursor, first: 2);
            context = new MockContext(pagingDetails);

            // act
            connection = (Connection)await pagingHandler.SliceAsync(context, list);

            // assert
            Assert.True(connection.Info.HasPreviousPage);
        }

        [Fact]
        public async Task HasPrevious_False()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new CursorPagingArguments(first: 1);
            var context = new MockContext(pagingDetails);

            // act
            var connection = (Connection)await pagingHandler.SliceAsync(context, list);

            // assert
            Assert.False(connection.Info.HasPreviousPage);
        }

        [Fact]
        public async Task Executable()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new MockExecutable<string>(new []
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g"
            });

            var pagingDetails = new CursorPagingArguments(2);
            var context = new MockContext(pagingDetails);

            // act
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
            IExtendedType sourceType = typeInspector.GetType(typeof(List<string>));

            IPagingProvider pagingProvider = new QueryableCursorPagingProvider();
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, default);

            var list = new MockExecutable<string>(new []
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g"
            }.AsQueryable());

            var pagingDetails = new CursorPagingArguments(2);
            var context = new MockContext(pagingDetails);

            // act
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

        public class MockContext : IResolverContext
        {
            private readonly CursorPagingArguments _arguments;

            public MockContext(CursorPagingArguments arguments)
            {
                _arguments = arguments;
            }

            public IServiceProvider Services => throw new NotImplementedException();

            public ISchema Schema => throw new NotImplementedException();

            public IObjectType RootType => throw new NotImplementedException();

            public IObjectType ObjectType => throw new NotImplementedException();

            public IObjectField Field => throw new NotImplementedException();

            public DocumentNode Document => throw new NotImplementedException();

            public OperationDefinitionNode Operation => throw new NotImplementedException();

            public FieldNode FieldSelection => throw new NotImplementedException();

            public IFieldSelection Selection => throw new NotImplementedException();

            public NameString ResponseName => throw new NotImplementedException();

            public Path Path => throw new NotImplementedException();

            public IImmutableDictionary<string, object> ScopedContextData
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public IImmutableDictionary<string, object> LocalContextData
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public IVariableValueCollection Variables => throw new NotImplementedException();

            public CancellationToken RequestAborted => default;

            public IDictionary<string, object> ContextData => throw new NotImplementedException();

            public T Argument<T>(NameString name)
            {
                throw new NotImplementedException();
            }

            public ValueKind ArgumentKind(NameString name)
            {
                throw new NotImplementedException();
            }

            public TValueNode ArgumentLiteral<TValueNode>(NameString name)
                where TValueNode : IValueNode
            {
                throw new NotImplementedException();
            }

            public Optional<T> ArgumentOptional<T>(NameString name)
            {
                throw new NotImplementedException();
            }

            public T ArgumentValue<T>(NameString name)
            {
                switch (name.Value)
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

                throw new NotSupportedException();
            }

            public IReadOnlyList<IFieldSelection> GetSelections(
                ObjectType typeContext,
                SelectionSetNode? selectionSet = null,
                bool allowInternals = false)
            {
                throw new NotImplementedException();
            }

            public T Parent<T>()
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

            public T Resolver<T>()
            {
                throw new NotImplementedException();
            }

            public T Service<T>()
            {
                throw new NotImplementedException();
            }

            public object Service(Type service)
            {
                throw new NotImplementedException();
            }
        }

        public class MockExecutable<T> : IExecutable<T>
            where T : class
        {
            private readonly IEnumerable<T> _source;

            public MockExecutable(IEnumerable<T> source)
            {
                _source = source;
            }

            public object Source => _source;

            public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
            {
                return new(_source.ToList());
            }

            public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
            {
                return new(_source.FirstOrDefault());
            }

            public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
            {
                return new(_source.SingleOrDefault());
            }

            public string Print()
            {
                return _source.ToString();
            }
        }
    }
}
