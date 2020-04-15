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


    public interface IResultData
    {
        IResultData? Parent { get; }
    }

    public interface IResultMap : IReadOnlyList<ResultValue>, IResultData
    {
    }

    public interface IResultMapList : IReadOnlyList<IResultMap>, IResultData
    {
    }

    public interface IResultValuePool
    {
        ResultValue[] Rent(int capacity);

        void Return(ResultValue[] buffer);
    }

    public sealed class ResultMap : IResultMap
    {
        private readonly ResultValue[] _buffer;
        private int _capacity;
        private bool _needsDefrag = false;

        public ResultMap(int capacity, ResultValue[] buffer)
        {
            _capacity = capacity;
            _buffer = buffer;
        }

        public IResultData? Parent { get; set; }

        public ResultValue this[int index] { get => _buffer[index]; }

        public int Count => _capacity;

        public void SetValue(int index, string name, object value)
        {
            if (index >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _buffer[index] = new ResultValue(name, value);
        }

        public void RemoveValue(int index)
        {
            _needsDefrag = true;
            _buffer[index] = default;
        }

        public void Complete()
        {
            if (!_needsDefrag)
            {
                return;
            }

            int count = 0;

            for (int i = 0; i < _capacity; i++)
            {
                bool moved = false;

                if (_buffer[i].HasValue)
                {
                    count = i + 1;
                }
                else
                {
                    for (int j = i + 1; j < _capacity; j++)
                    {
                        if (_buffer[j].HasValue)
                        {
                            _buffer[i] = _buffer[j];
                            moved = true;
                            break;
                        }
                    }

                    if (moved)
                    {
                        count = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _capacity = count;
        }

        public IEnumerator<ResultValue> GetEnumerator()
        {
            for (int i = 0; i < _capacity; i++)
            {
                ResultValue value = _buffer[i];

                if (value.HasValue)
                {
                    yield return value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ResultMapList
        : List<IResultMap>
        , IResultMapList
    {
        public IResultData? Parent { get; set; }
    }

    public readonly struct ResultValue : IEquatable<ResultValue?>
    {
        public ResultValue(string name, object value)
        {
            Name = name;
            Value = value;
            HasValue = true;
        }

        public string Name { get; }

        public object Value { get; }

        public bool HasValue { get; }

        public override bool Equals(object? obj)
        {
            return obj is FieldValue value &&
                HasValue == value.HasValue &&
                Name == value.Key &&
                Value == value.Value;
        }

        public bool Equals(ResultValue? other)
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

            return Name == other.Value.Name &&
                   Value == other.Value.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Name?.GetHashCode() ?? 0) * 3;
                hash = hash ^ ((Value?.GetHashCode() ?? 0) * 7);
                return hash;
            }
        }
    }

    public interface IResult : IDisposable
    {
        IResultMap? Data { get; }
    }

    internal class Result : IResult
    {
        public IResultMap? Data { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class ListBuffer<T>
    {
        private readonly List<T>[] _buffer = new List<T>[]
        {
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
            new List<T>(),
        };
        private readonly int _max = 16;
        private int _index = 0;


        public IList<T> Pop()
        {
            if (TryPop(out IList<T>? list))
            {
                return list;
            }
            throw new InvalidOperationException("Buffer is used up.");
        }

        public bool TryPop([NotNullWhen(true)] out IList<T>? list)
        {
            if (_index < _max)
            {
                list = _buffer[_index++];
                return true;
            }

            list = null;
            return false;
        }

        public void Reset()
        {
            if (_index > 0)
            {
                for (int i = 0; i < _index; i++)
                {
                    _buffer[i].Clear();
                }
            }
            _index = 0;
        }
    }
}
