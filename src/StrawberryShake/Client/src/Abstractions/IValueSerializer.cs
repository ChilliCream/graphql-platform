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



    public interface IOperationResult
    {
        object? Data { get; }

        IReadOnlyList<IError> Errors { get; }

        IReadOnlyDictionary<string, object?> Extensions { get; }

        Type ResultType { get; }

        bool HasErrors { get; }

        void EnsureNoErrors();
    }

    public interface IOperationResult<T> : IOperationResult where T : class
    {
        new T? Data { get; }
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

    public interface IOperationObservable<T> : IObservable<IOperationResult<T>> where T : class
    {
        void Subscribe(
            Action<IOperationResult<T>> next,
            CancellationToken cancellationToken = default);
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


    public class Foo
    {
        public string Bar { get; }

        public Baz Baz { get;  }
    }

    public class Baz
    {
        public string Quox { get; }
    }

    public class FooEntity
    {
        public string Id { get; set; }
        public string Bar { get; set; }
        public EntityId Baz { get; set; }
        public List<EntityId> Bars { get; set; }
    }

    public interface IEntityStore
    {
        T GetOrCreate<T>(EntityId id) where T : class;

        IDisposable BeginUpdate();
    }

    public interface IOperationStore
    {
        // IRequest / IOperationResult / [EntityId] / IOperationObservable

        void Set<T>(IRequest request, IOperationResult<T> result) where T : class;

        IOperationResult<T> Get<T>(IRequest request) where T : class;

        IOperationObservable<T> Subscribe<T>(IRequest request) where T : class;
    }



    public readonly struct EntityId
    {
        public EntityId(string name, object value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the GraphQL type name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the internal ID value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Indicates whether this instance and a specified <paramref name="other"/> are equal.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="EntityId"/> to compare with the current instance.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="other" /> and this instance are
        /// the same type and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(EntityId other) =>
            Name == other.Name && Value.Equals(other.Value);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current instance.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are
        /// the same type and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj) =>
            obj is EntityId other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() =>
            HashCode.Combine(Name, Value);

        public void Deconstruct(out string name, out object value)
        {
            name = Name;
            value = Value;
        }
    }

    public enum ValueKind
    {
        String,
        Integer,
        Float,
        Boolean,
        Enum,
        Object
    }
}
