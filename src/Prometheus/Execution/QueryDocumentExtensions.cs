using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    internal static class QueryDocumentExtensions
    {
        public static OperationDefinition GetOperation(this QueryDocument document, string name)
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
}