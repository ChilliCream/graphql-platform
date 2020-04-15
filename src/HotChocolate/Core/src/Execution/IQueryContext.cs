using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryContext : IHasContextData
    {
        /// <summary>
        /// Gets the GraphQL schema on which the query is executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets or sets the initial query request.
        /// </summary>
        IReadOnlyQueryRequest Request { get; set; }

        /// <summary>
        /// Gets or sets the scoped request services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets or sets the parsed query document.
        /// </summary>
        DocumentNode Document { get; set; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted
        /// and thus request operations should be cancelled.
        /// </summary>
        CancellationToken RequestAborted { get; set; }

        /// <summary>
        /// Gets or sets an unexpected execution exception.
        /// </summary>
        Exception Exception { get; set; }
    }


    public interface IResultData : IDisposable
    {
        IResultData? Parent { get; }
    }

    public interface IResultMap : IResultData, IReadOnlyList<ResultValue>
    {

    }

    public class ResultMap : IResultMap
    {
        private readonly List<ResultValue> _values = new List<ResultValue>();

        public ResultValue this[int index] { get => _values[index]; }

        public IResultData? Parent => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ResultValue> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public readonly struct ResultValue : IEquatable<ResultValue?>
    {
        internal ResultValue(string key, object value)
        {
            Key = key;
            Value = value;
            HasValue = true;
        }

        public string Key { get; }

        public object Value { get; }

        public bool HasValue { get; }

        public override bool Equals(object? obj)
        {
            return obj is FieldValue value &&
                HasValue == value.HasValue &&
                Key == value.Key &&
                Value == value.Value;
        }

        public bool Equals(FieldValue? other)
        {
            if (other is null)
            {
                return false;
            }

            if (HasValue != other.Value.HasValue)
            {
                return false;
            }

            if (HasValue == false)
            {
                return true;
            }

            return Key == other.Value.Key &&
                   Value == other.Value.Value;
        }

        public bool Equals([AllowNull] ResultValue? other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Key?.GetHashCode() ?? 0) * 3;
                hash = hash ^ ((Value?.GetHashCode() ?? 0) * 7);
                return hash;
            }
        }
    }
}
