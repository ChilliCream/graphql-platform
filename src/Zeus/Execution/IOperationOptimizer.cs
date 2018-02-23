using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{

    public class OperationOptimizer
        : IOperationOptimizer
    {
        public IOptimizedOperation Optimize(ISchema schema, QueryDocument document, string operationName)
        {
          
            throw new NotImplementedException();
        }


        private static OperationDefinition GetOperation(QueryDocument document, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (document.Operations.Count == 1)
                {
                    return document.Operations.Values.First();
                }
                throw new Exception("TODO: Query Exception");
            }
            else
            {
                if (document.Operations.TryGetValue(name, out var operation))
                {
                    return operation;
                }
                throw new Exception("TODO: Query Exception");
            }
        }


    }

    public interface IOperationOptimizer
    {
        IOptimizedOperation Optimize(ISchema schema, QueryDocument queryDocument, string operationName);
    }
}