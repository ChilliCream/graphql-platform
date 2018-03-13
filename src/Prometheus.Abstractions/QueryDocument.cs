using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Prometheus.Abstractions
{
    public class QueryDocument
        : IQueryDocument
    {
        private readonly IQueryDefinition[] _queryDefinitions;
        private readonly IReadOnlyDictionary<string, OperationDefinition> _operations;
        private ILookup<string, FragmentDefinition> _fragments;

        private string _stringRepresentation;

        public QueryDocument(IEnumerable<IQueryDefinition> queryDefinitions)
        {
            if (queryDefinitions == null)
            {
                throw new ArgumentNullException(nameof(queryDefinitions));
            }

            _queryDefinitions = queryDefinitions.ToArray();

            Dictionary<string, OperationDefinition> operations =
                new Dictionary<string, OperationDefinition>();
            foreach (var operation in queryDefinitions.OfType<OperationDefinition>())
            {
                operations[operation.Name] = operation;
            }

            _operations = operations;
            _fragments = queryDefinitions.OfType<FragmentDefinition>()
                .ToLookup(t => t.Name, StringComparer.Ordinal);
        }
     
        public OperationDefinition GetOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                if (_operations.Count == 1)
                {
                    return _operations.Values.First();
                }
                throw new GraphQLException("The specified query has more than "
                    + "one operation. The operation name has to be specified.");
            }
            else
            {
                if (_operations.TryGetValue(operationName, out var operation))
                {
                    return operation;
                }
                throw new GraphQLException(
                    $"The specified operation ({operationName}) does not exist.");
            }
        }

        public FragmentDefinition GetFragment(string fragmentName, NamedType type)
        {
            if (string.IsNullOrEmpty(fragmentName))
            {
                throw new ArgumentException("message", nameof(fragmentName));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _fragments[fragmentName].FirstOrDefault(
                t => IsEqual(t.TypeCondition, type));
        }

        public IEnumerator<IQueryDefinition> GetEnumerator()
        {
            return _queryDefinitions.OfType<IQueryDefinition>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");

                foreach (IQueryDefinition definition in _operations.Values)
                {
                    sb.AppendLine(definition.ToString());
                }

                foreach (IQueryDefinition definition in _operations.Values)
                {
                    sb.AppendLine(definition.ToString());
                }

                sb.AppendLine("}");
                _stringRepresentation = sb.ToString();
            }
            return _stringRepresentation;
        }

        private static bool IsEqual(NamedType x, NamedType y)
        {
            return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}