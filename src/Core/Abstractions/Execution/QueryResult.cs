﻿using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class QueryResult
        : IQueryResult
    {
        private readonly OrderedDictionary _data
            = new OrderedDictionary();
        private readonly OrderedDictionary _extensions
            = new OrderedDictionary();
        private readonly List<IError> _errors
            = new List<IError>();

        public IDictionary<string, object> Data => _data;

        public IDictionary<string, object> Extensions => _extensions;

        public ICollection<IError> Errors => _errors;

        IReadOnlyDictionary<string, object> IReadOnlyQueryResult.Data =>
            _data;

        IReadOnlyDictionary<string, object> IExecutionResult.Extensions =>
            _extensions;

        IReadOnlyCollection<IError> IExecutionResult.Errors =>
            _errors;

        public IReadOnlyQueryResult AsReadOnly()
        {
            return new ReadOnlyQueryResult(this);
        }

        public static QueryResult CreateError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            var result = new QueryResult();
            result.Errors.Add(error);
            return result;
        }

        public static QueryResult CreateError(IEnumerable<IError> error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            var result = new QueryResult();
            result._errors.AddRange(error);
            return result;
        }
    }
}
