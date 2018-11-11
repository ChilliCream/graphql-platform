using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Execution
{
    internal class QueryResult
        : IQueryExecutionResult
    {
        private const string _data = "data";
        private const string _errors = "errors";

        public QueryResult(OrderedDictionary data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Data = data;
            Data.MakeReadOnly();
        }

        public QueryResult(params IQueryError[] errors)
            : this(errors as IEnumerable<IQueryError>)
        {
        }

        public QueryResult(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToImmutableList();

            if (Errors.Count == 0)
            {
                throw new ArgumentException("The list of errors ");
            }
        }

        public QueryResult(
            OrderedDictionary data,
            IEnumerable<IQueryError> errors)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Data = data;
            Data.MakeReadOnly();

            Errors = new List<IQueryError>(errors).AsReadOnly();
            if (Errors.Count == 0)
            {
                Errors = null;
            }
        }

        public OrderedDictionary Data { get; }

        IOrderedDictionary IQueryExecutionResult.Data => Data;

        public IReadOnlyCollection<IQueryError> Errors { get; }

        public T ToObject<T>()
        {
            throw new NotImplementedException();
        }

        public string ToJson()
        {
            return ToJson(false);
        }

        public string ToJson(bool indented)
        {
            var internalResult = new Dictionary<string, object>();

            if (Errors != null && Errors.Count > 0)
            {
                internalResult[_errors] = Errors;
            }

            if (Data != null && Data.Count > 0)
            {
                internalResult[_data] = Data;
            }

            return JsonConvert.SerializeObject(
                internalResult,
                indented ? Formatting.Indented : Formatting.None);
        }

        public IReadOnlyDictionary<string, object> ToDictionary()
        {
            var internalResult = new Dictionary<string, object>();

            if (Errors != null && Errors.Count > 0)
            {
                internalResult[_errors] = Errors;
            }

            if (Data != null && Data.Count > 0)
            {
                internalResult[_data] = Data;
            }

            return internalResult;
        }

        public override string ToString()
        {
            return ToJson(true);
        }
    }


}
