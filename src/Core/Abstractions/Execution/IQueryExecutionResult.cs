using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public class ReadOnlyQueryResult
        : IReadOnlyQueryResult
    {
        public ReadOnlyQueryResult()
        {

        }

        public IReadOnlyDictionary<string, object> Data => throw new System.NotImplementedException();

        public IReadOnlyCollection<IError> Errors => throw new System.NotImplementedException();

        public IReadOnlyDictionary<string, object> Extensions => throw new System.NotImplementedException();
    }

    public class QueryResult
        : IQueryResult
    {
        private readonly OrderedDictionary _data;
        private readonly OrderedDictionary _extensions;
        private readonly List<IError> _errors;

        public IDictionary<string, object> Data => throw new System.NotImplementedException();

        public IDictionary<string, object> Extensions => throw new System.NotImplementedException();

        public ICollection<IError> Errors => throw new System.NotImplementedException();

        IReadOnlyDictionary<string, object> IReadOnlyQueryResult.Data => throw new System.NotImplementedException();

        IReadOnlyCollection<IError> IExecutionResult.Errors => throw new System.NotImplementedException();

        IReadOnlyDictionary<string, object> IExecutionResult.Extensions => throw new System.NotImplementedException();

        public IReadOnlyQueryResult AsReadOnly()
        {
            throw new System.NotImplementedException();
        }
    }
}
