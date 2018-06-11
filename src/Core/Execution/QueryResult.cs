using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public class QueryResult
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
        }

        public QueryResult(List<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }
            Errors = errors.ToImmutableList();
        }

        public QueryResult(OrderedDictionary data, List<IQueryError> errors)
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
            Errors = errors.ToImmutableList();
        }

        public OrderedDictionary Data { get; }
        public ImmutableList<IQueryError> Errors { get; }

        public string ToString(bool indented)
        {
            Dictionary<string, object> internalResult = new Dictionary<string, object>();

            if (Errors != null)
            {
                internalResult[_errors] = Errors;
            }

            if (Data != null && Data.Count > 0)
            {
                internalResult[_data] = Data;
            }

            return JsonConvert.SerializeObject(internalResult,
                indented ? Formatting.Indented : Formatting.None);
        }

        public override string ToString()
        {
            return ToString(true);
        }
    }
}
