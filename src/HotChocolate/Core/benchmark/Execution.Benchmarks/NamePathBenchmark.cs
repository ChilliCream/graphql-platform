#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class NamePathBenchmark
{
    private readonly NameString _name = "name";
    private readonly Pooled.PathFactory _pathFactory;

    public NamePathBenchmark()
    {
        var indexerPathPool = new Pooled.IndexerPathSegmentPool(256);
        var namePathPool = new Pooled.NamePathSegmentPool(256);
        _pathFactory = new Pooled.PathFactory(indexerPathPool, namePathPool);
    }

    [Benchmark]
    public void V12_CreateNotManyPaths()
    {
        V12.Path root = V12.RootPathSegment.Instance;
        for (var rootFieldCount = 0; rootFieldCount < 1; rootFieldCount++)
        {
            V12.Path rootField = root.Append(_name);
            for (var arrayCount = 0; arrayCount < 10; arrayCount++)
            {
                V12.Path arrayField = rootField.Append(arrayCount);
                for (var leafCount = 0; leafCount < 10; leafCount++)
                {
                    arrayField.Append(_name);
                }
            }
        }
    }

    [Benchmark]
    public void V12_CreateManyNamedPaths()
    {
        V12.Path root = V12.RootPathSegment.Instance;
        for (var rootFieldCount = 0; rootFieldCount < 10; rootFieldCount++)
        {
            V12.Path rootField = root.Append(_name);
            for (var subFieldCount = 0; subFieldCount < 10; subFieldCount++)
            {
                V12.Path subField = rootField.Append(_name);
                for (var arrayCount = 0; arrayCount < 10; arrayCount++)
                {
                    V12.Path arrayField = subField.Append(arrayCount);
                    for (var leafCount = 0; leafCount < 10; leafCount++)
                    {
                        arrayField.Append(_name);
                    }
                }
            }
        }
    }

    //[Benchmark]
    public async Task V12_CreateManyNamedPaths_Async()
    {
        void Create()
        {
            V12.Path root = V12.RootPathSegment.Instance;
            for (var rootFieldCount = 0; rootFieldCount < 10; rootFieldCount++)
            {
                V12.Path rootField = root.Append(_name);
                for (var subFieldCount = 0; subFieldCount < 10; subFieldCount++)
                {
                    V12.Path subField = rootField.Append(_name);
                    for (var arrayCount = 0; arrayCount < 10; arrayCount++)
                    {
                        V12.Path arrayField = subField.Append(arrayCount);
                        for (var leafCount = 0; leafCount < 10; leafCount++)
                        {
                            arrayField.Append(_name);
                        }
                    }
                }
            }
        }

        Task[] tasks = { Task.Run(Create), Task.Run(Create), Task.Run(Create), };
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public void Pooled_CreateManyNamedPaths()
    {
        Pooled.Path root = Pooled.RootPathSegment.Instance;
        for (var rootFieldCount = 0; rootFieldCount < 10; rootFieldCount++)
        {
            Pooled.Path rootField = _pathFactory.Append(root, _name);
            for (var subFieldCount = 0; subFieldCount < 10; subFieldCount++)
            {
                Pooled.Path subField = _pathFactory.Append(rootField, _name);
                for (var arrayCount = 0; arrayCount < 10; arrayCount++)
                {
                    Pooled.Path arrayField = _pathFactory.Append(subField, arrayCount);
                    for (var leafCount = 0; leafCount < 10; leafCount++)
                    {
                        _pathFactory.Append(arrayField, _name);
                    }
                }
            }
        }

        _pathFactory.Clear();
    }

    [Benchmark]
    public void Pooled_CreateNotManyPaths()
    {
        Pooled.Path root = Pooled.RootPathSegment.Instance;
        for (var rootFieldCount = 0; rootFieldCount < 1; rootFieldCount++)
        {
            Pooled.Path rootField = _pathFactory.Append(root, _name);
            for (var arrayCount = 0; arrayCount < 10; arrayCount++)
            {
                Pooled.Path arrayField = _pathFactory.Append(rootField, arrayCount);
                for (var leafCount = 0; leafCount < 10; leafCount++)
                {
                    _pathFactory.Append(arrayField, _name);
                }
            }
        }
        _pathFactory.Clear();
    }

    //[Benchmark]
    public async Task Pooled_CreateManyNamedPaths_Async()
    {
        void Create()
        {
            Pooled.Path root = Pooled.RootPathSegment.Instance;
            for (var rootFieldCount = 0; rootFieldCount < 10; rootFieldCount++)
            {
                Pooled.Path rootField = _pathFactory.Append(root, _name);
                for (var subFieldCount = 0; subFieldCount < 10; subFieldCount++)
                {
                    Pooled.Path subField = _pathFactory.Append(rootField, _name);
                    for (var arrayCount = 0; arrayCount < 10; arrayCount++)
                    {
                        Pooled.Path arrayField = _pathFactory.Append(subField, arrayCount);
                        for (var leafCount = 0; leafCount < 10; leafCount++)
                        {
                            _pathFactory.Append(arrayField, _name);
                        }
                    }
                }
            }
        }

        Task[] tasks = { Task.Run(Create), Task.Run(Create), Task.Run(Create), };
        await Task.WhenAll(tasks);
        _pathFactory.Clear();
    }

    public static class V12
    {
        #region v12

        public abstract class Path : IEquatable<Path>
        {
            internal Path() { }

            /// <summary>
            /// Gets the parent path segment.
            /// </summary>
            public abstract Path? Parent { get; }

            /// <summary>
            /// Gets the count of segments this path contains.
            /// </summary>
            public abstract int Depth { get; }

            /// <summary>
            /// Appends an element.
            /// </summary>
            /// <param name="index">The index of the element.</param>
            /// <returns>Returns a new path segment pointing to an element in a list.</returns>
            public virtual IndexerPathSegment Append(int index)
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return new IndexerPathSegment(this, index);
            }

            /// <summary>
            /// Appends a new path segment.
            /// </summary>
            /// <param name="name">The name of the path segment.</param>
            /// <returns>Returns a new path segment.</returns>
            public virtual NamePathSegment Append(NameString name)
            {
                name.EnsureNotEmpty(nameof(name));
                return new NamePathSegment(this, name);
            }

            /// <summary>
            /// Generates a string that represents the current path.
            /// </summary>
            /// <returns>
            /// Returns a string that represents the current path.
            /// </returns>
            public abstract string Print();

            /// <summary>
            /// Creates a new list representing the current <see cref="Path"/>.
            /// </summary>
            /// <returns>
            /// Returns a new list representing the current <see cref="Path"/>.
            /// </returns>
            public IReadOnlyList<object> ToList()
            {
                if (this is RootPathSegment)
                {
                    return Array.Empty<object>();
                }

                var stack = new List<object>();
                Path? current = this;

                while (current != null)
                {
                    switch (current)
                    {
                        case IndexerPathSegment indexer:
                            stack.Insert(0, indexer.Index);
                            break;

                        case NamePathSegment name:
                            stack.Insert(0, name.Name);
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    current = current.Parent;
                }

                return stack;
            }

            /// <summary>Returns a string that represents the current <see cref="Path"/>.</summary>
            /// <returns>A string that represents the current <see cref="Path"/>.</returns>
            public override string ToString() => Print();

            public abstract bool Equals(Path? other);

            public sealed override bool Equals(object? obj)
                => obj switch
                {
                    null => false,
                    Path p => Equals(p),
                    _ => false
                };

            /// <summary>
            /// Serves as the default hash function.
            /// </summary>
            /// <returns>
            /// A hash code for the current <see cref="Path"/>.
            /// </returns>
            public abstract override int GetHashCode();

            /// <summary>
            /// Creates a root segment.
            /// </summary>
            /// <param name="name">The name of the root segment.</param>
            /// <returns>
            /// Returns a new root segment.
            /// </returns>
            public static NamePathSegment New(NameString name) => new(null, name);

            public static RootPathSegment Root => RootPathSegment.Instance;

            internal static Path FromList(IReadOnlyList<object> path)
            {
                if (path is null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (path.Count == 0)
                {
                    return Root;
                }

                Path segment = New((string)path[0]);

                for (var i = 1; i < path.Count; i++)
                {
                    segment = path[i] switch
                    {
                        NameString n => segment.Append(n),
                        string s => segment.Append(s),
                        int n => segment.Append(n),
                        _ => throw new NotSupportedException("notsupported")
                    };
                }

                return segment;
            }
        }

        public sealed class NamePathSegment : Path
        {
            public NamePathSegment(Path? parent, NameString name)
            {
                Parent = parent;
                Depth = parent?.Depth + 1 ?? 0;
                Name = name;
            }

            /// <inheritdoc />
            public override Path? Parent { get; }

            /// <inheritdoc />
            public override int Depth { get; }

            /// <summary>
            ///  Gets the name representing a field on a result map.
            /// </summary>
            public NameString Name { get; }

            /// <inheritdoc />
            public override string Print()
            {
                var parent = Parent is null ? string.Empty : Parent.Print();
                return $"{parent}/{Name}";
            }

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                if (other is NamePathSegment name &&
                    Depth.Equals(name.Depth) &&
                    Name.Equals(name.Name))
                {
                    if (Parent is null)
                    {
                        return name.Parent is null;
                    }

                    if (name.Parent is null)
                    {
                        return false;
                    }

                    return Parent.Equals(name.Parent);
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (Parent?.GetHashCode() ?? 0) * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Name.GetHashCode() * 11;
                    return hash;
                }
            }
        }

        /// <summary>
        /// An <see cref="IndexerPathSegment" /> represents a pointer to
        /// an list element in the result structure.
        /// </summary>
        public sealed class IndexerPathSegment : Path
        {
            internal IndexerPathSegment(Path parent, int index)
            {
                Parent = parent;
                Depth = parent.Depth + 1;
                Index = index;
            }

            /// <inheritdoc />
            public override Path Parent { get; }

            /// <inheritdoc />
            public override int Depth { get; }

            /// <summary>
            /// Gets the <see cref="Index"/> which represents the position an element in a
            /// list of the result structure.
            /// </summary>
            public int Index { get; }

            /// <inheritdoc />
            public override string Print()
            {
                return $"{Parent.Print()}[{Index}]";
            }

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                if (other is IndexerPathSegment indexer &&
                    Depth.Equals(indexer.Depth) &&
                    Index.Equals(indexer.Index) &&
                    Parent.Equals(indexer.Parent))
                {
                    return true;
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Parent.GetHashCode() * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Index.GetHashCode() * 11;
                    return hash;
                }
            }
        }

        public sealed class RootPathSegment : Path
        {
            private RootPathSegment()
            {
                Parent = null;
                Depth = 0;
                Name = default;
            }

            /// <inheritdoc />
            public override Path? Parent { get; }

            /// <inheritdoc />
            public override int Depth { get; }

            /// <summary>
            ///  Gets the name representing a field on a result map.
            /// </summary>
            public NameString Name { get; }

            /// <inheritdoc />
            public override IndexerPathSegment Append(int index) =>
                throw new NotSupportedException();

            /// <inheritdoc />
            public override NamePathSegment Append(NameString name) =>
                New(name);

            /// <inheritdoc />
            public override string Print() => "/";

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return ReferenceEquals(other, this);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (Parent?.GetHashCode() ?? 0) * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Name.GetHashCode() * 11;
                    return hash;
                }
            }

            public static RootPathSegment Instance { get; } = new RootPathSegment();
        }

        #endregion
    }

    public static class Pooled
    {
        #region Pooled

        public sealed class IndexerPathSegmentPool
            : DefaultObjectPool<PathSegmentBuffer<IndexerPathSegment>>
        {
            public IndexerPathSegmentPool(int maximumRetained)
                : base(new BufferPolicy(), maximumRetained)
            {
            }

            private sealed class BufferPolicy
                : IPooledObjectPolicy<PathSegmentBuffer<IndexerPathSegment>>
            {
                private static readonly IndexerPathSegmentPolicy _policy = new();

                public PathSegmentBuffer<IndexerPathSegment> Create() => new(256, _policy);

                public bool Return(PathSegmentBuffer<IndexerPathSegment> obj)
                {
                    obj.Reset();
                    return true;
                }
            }

            private sealed class IndexerPathSegmentPolicy : IPooledObjectPolicy<IndexerPathSegment>
            {
                public IndexerPathSegment Create() => new();

                public bool Return(IndexerPathSegment segment)
                {
                    segment.Parent = null!;
                    return true;
                }
            }
        }

        public sealed class NamePathSegmentPool
            : DefaultObjectPool<PathSegmentBuffer<NamePathSegment>>
        {
            public NamePathSegmentPool(int maximumRetained)
                : base(new BufferPolicy(), maximumRetained)
            {
            }

            private sealed class BufferPolicy
                : IPooledObjectPolicy<PathSegmentBuffer<NamePathSegment>>
            {
                private static readonly NamePathSegmentPolicy _policy = new();

                public PathSegmentBuffer<NamePathSegment> Create() => new(256, _policy);

                public bool Return(PathSegmentBuffer<NamePathSegment> obj)
                {
                    obj.Reset();
                    return true;
                }
            }

            private sealed class NamePathSegmentPolicy : IPooledObjectPolicy<NamePathSegment>
            {
                public NamePathSegment Create() => new();

                public bool Return(NamePathSegment segment)
                {
                    segment.Name = null!;
                    segment.Parent = null!;
                    return true;
                }
            }
        }

        public class PathSegmentFactory<T> where T : class
        {
            private readonly DefaultObjectPool<PathSegmentBuffer<T>> _pool;
            private PathSegmentBuffer<T>? _current;
            private List<PathSegmentBuffer<T>>? _buffers;
            private object lockObject = new();

            public PathSegmentFactory(DefaultObjectPool<PathSegmentBuffer<T>> pool)
            {
                _pool = pool;
            }

            private void Resize()
            {
                if (_current is null || !_current.HasSpace())
                {
                    lock (lockObject)
                    {
                        if (_current is null || !_current.HasSpace())
                        {
                            if (_current is not null)
                            {
                                _buffers ??= new();
                                _buffers.Add(_current);
                            }

                            _current = _pool.Get();
                        }
                    }
                }
            }

            public T Get()
            {
                while (true)
                {
                    if (_current is null || !_current.TryPop(out T? segment))
                    {
                        Resize();
                        continue;
                    }

                    return segment;
                }
            }

            public void Clear()
            {
                if (_buffers is not null)
                {
                    for (var i = 0; i < _buffers.Count; i++)
                    {
                        _pool.Return(_buffers[i]);
                    }

                    _buffers.Clear();
                }

                if (_current is not null)
                {
                    _pool.Return(_current);
                }
            }
        }

        public class PathFactory
        {
            private readonly PathSegmentFactory<IndexerPathSegment> _indexerPathFactory;
            private readonly PathSegmentFactory<NamePathSegment> _namePathFactory;

            public PathFactory(
                IndexerPathSegmentPool indexerPathPool,
                NamePathSegmentPool namePathPool)
            {
                _indexerPathFactory = new PathSegmentFactory<IndexerPathSegment>(indexerPathPool);
                _namePathFactory = new PathSegmentFactory<NamePathSegment>(namePathPool);
            }

            /// <summary>
            /// Appends an element.
            /// </summary>
            /// <param name="parent">The parent</param>
            /// <param name="index">The index of the element.</param>
            /// <returns>Returns a new path segment pointing to an element in a list.</returns>
            public virtual IndexerPathSegment Append(Path? parent, int index)
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                IndexerPathSegment indexer = _indexerPathFactory.Get();

                indexer.Parent = parent;
                indexer.Index = index;

                return indexer;
            }

            /// <summary>
            /// Appends a new path segment.
            /// </summary>
            /// <param name="name">The name of the path segment.</param>
            /// <returns>Returns a new path segment.</returns>
            public virtual NamePathSegment Append(Path? parent, NameString name)
            {
                // TODO private
                //name.EnsureNotEmpty(nameof(name));

                NamePathSegment indexer = _namePathFactory.Get();

                indexer.Parent = parent;
                indexer.Name = name;

                return indexer;
            }

            public void Clear()
            {
                _indexerPathFactory.Clear();
                _namePathFactory.Clear();
            }
        }

        public abstract class Path : IEquatable<Path>
        {
            private Path? _parent;

            /// <summary>
            /// Gets the parent path segment.
            /// </summary>
            public Path? Parent
            {
                get => _parent;
                internal set
                {
                    if (_parent is { })
                    {
                        Depth = _parent.Depth + 1;
                    }
                    else
                    {
                        Depth = 0;
                    }

                    _parent = value;
                }
            }

            /// <summary>
            /// Gets the count of segments this path contains.
            /// </summary>
            public int Depth { get; protected set; }

            /// <summary>
            /// Generates a string that represents the current path.
            /// </summary>
            /// <returns>
            /// Returns a string that represents the current path.
            /// </returns>
            public abstract string Print();

            /// <summary>
            /// Creates a new list representing the current <see cref="Path"/>.
            /// </summary>
            /// <returns>
            /// Returns a new list representing the current <see cref="Path"/>.
            /// </returns>
            public IReadOnlyList<object> ToList()
            {
                if (this is RootPathSegment)
                {
                    return Array.Empty<object>();
                }

                var stack = new List<object>();
                Path? current = this;

                while (current != null)
                {
                    switch (current)
                    {
                        case IndexerPathSegment indexer:
                            stack.Insert(0, indexer.Index);
                            break;

                        case NamePathSegment name:
                            stack.Insert(0, name.Name);
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    current = current.Parent;
                }

                return stack;
            }

            /// <summary>Returns a string that represents the current <see cref="Path"/>.</summary>
            /// <returns>A string that represents the current <see cref="Path"/>.</returns>
            public override string ToString() => Print();

            public abstract bool Equals(Path? other);

            public sealed override bool Equals(object? obj)
                => obj switch
                {
                    null => false,
                    Path p => Equals(p),
                    _ => false
                };

            /// <summary>
            /// Serves as the default hash function.
            /// </summary>
            /// <returns>
            /// A hash code for the current <see cref="Path"/>.
            /// </returns>
            public abstract override int GetHashCode();

            public static RootPathSegment Root => RootPathSegment.Instance;

            internal static Path FromList(IReadOnlyList<object> path)
            {
                if (path is null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (path.Count == 0)
                {
                    return Root;
                }

                //Path segment = New((string)path[0]);


                /*
                 TODO i guess here we can just not pool the object
                for (var i = 1; i < path.Count; i++)
                {
                    segment = path[i] switch
                    {
                        NameString n => segment.Append(n),
                        string s => segment.Append(s),
                        int n => segment.Append(n),
                        _ => throw new NotSupportedException("notsupported")
                    };
                }
                */

                return RootPathSegment.Instance;
            }
        }

        public sealed class NamePathSegment : Path
        {
            /// <summary>
            ///  Gets the name representing a field on a result map.
            /// </summary>
            public NameString Name { get; internal set; }

            /// <inheritdoc />
            public override string Print()
            {
                var parent = Parent is null ? string.Empty : Parent.Print();
                return $"{parent}/{Name}";
            }

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                if (other is NamePathSegment name &&
                    Depth.Equals(name.Depth) &&
                    Name.Equals(name.Name))
                {
                    if (Parent is null)
                    {
                        return name.Parent is null;
                    }

                    if (name.Parent is null)
                    {
                        return false;
                    }

                    return Parent.Equals(name.Parent);
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (Parent?.GetHashCode() ?? 0) * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Name.GetHashCode() * 11;
                    return hash;
                }
            }
        }

        /// <summary>
        /// An <see cref="IndexerPathSegment" /> represents a pointer to
        /// an list element in the result structure.
        /// </summary>
        public sealed class IndexerPathSegment : Path
        {
            /// <summary>
            /// Gets the <see cref="Index"/> which represents the position an element in a
            /// list of the result structure.
            /// </summary>
            public int Index { get; internal set; }

            /// <inheritdoc />
            public override string Print()
            {
                return $"{Parent!.Print()}[{Index}]";
            }

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                if (other is IndexerPathSegment indexer &&
                    Depth.Equals(indexer.Depth) &&
                    Index.Equals(indexer.Index) &&
                    Parent.Equals(indexer.Parent))
                {
                    return true;
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Parent.GetHashCode() * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Index.GetHashCode() * 11;
                    return hash;
                }
            }
        }

        public sealed class RootPathSegment : Path
        {
            private RootPathSegment()
            {
                Parent = null;
                Depth = 0;
                Name = default;
            }

            /// <summary>
            ///  Gets the name representing a field on a result map.
            /// </summary>
            public NameString Name { get; }

            /// <inheritdoc />
            public override string Print() => "/";

            /// <inheritdoc />
            public override bool Equals(Path? other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return ReferenceEquals(other, this);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (Parent?.GetHashCode() ?? 0) * 3;
                    hash ^= Depth.GetHashCode() * 7;
                    hash ^= Name.GetHashCode() * 11;
                    return hash;
                }
            }

            public static RootPathSegment Instance { get; } = new RootPathSegment();
        }

        /// <summary>
        /// Ala <see cref="ResultObjectBuffer{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public sealed class PathSegmentBuffer<T> where T : class
        {
            private readonly int _capacity;
            private readonly IPooledObjectPolicy<T> _policy;
            private readonly T?[] _buffer;
            private int _index;

            public PathSegmentBuffer(int capacity, IPooledObjectPolicy<T> policy)
            {
                _capacity = capacity;
                _policy = policy;
                _buffer = new T[capacity];
            }

            public bool HasSpace() => _index < _capacity;

            public T Pop()
            {
                if (TryPop(out T? obj))
                {
                    return obj;
                }

                throw new InvalidOperationException("Buffer is used up.");
            }

            public bool TryPop([NotNullWhen(true)] out T? obj)
            {
                var nextIndex = _index++;
                if (nextIndex < _capacity)
                {
                    if (_buffer[nextIndex] is { } o)
                    {
                        obj = o;
                        return true;
                    }

                    obj = _policy.Create();
                    _buffer[nextIndex] = obj;
                    return true;
                }

                obj = null;
                return false;
            }

            public void Reset()
            {
                if (_index == 0)
                {
                    return;
                }

                if (_index >= _capacity)
                {
                    _index = _capacity;
                }

                for (var i = 0; i < _index; i++)
                {
                    if (!_policy.Return(_buffer[i]!))
                    {
                        _buffer[i] = null;
                    }
                }

                _index = 0;
            }
        }

        #endregion
    }
}
