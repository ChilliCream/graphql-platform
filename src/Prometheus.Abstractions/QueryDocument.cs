using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Prometheus.Abstractions
{
    public class QueryDocument
    {
        private string _stringRepresentation;

        public QueryDocument(IEnumerable<IQueryDefinition> queryDefinitions)
        {
            if (queryDefinitions == null)
            {
                throw new System.ArgumentNullException(nameof(queryDefinitions));
            }

            Operations = queryDefinitions.OfType<OperationDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);
            Fragments = queryDefinitions.OfType<FragmentDefinition>()
                .ToLookup(t => t.Name, StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, OperationDefinition> Operations { get; }

        public ILookup<string, FragmentDefinition> Fragments { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                
                foreach (IQueryDefinition definition in Operations.Values)
                {
                    sb.AppendLine(definition.ToString());
                }
                
                foreach (IQueryDefinition definition in Operations.Values)
                {
                    sb.AppendLine(definition.ToString());
                }

                sb.AppendLine("}");
                _stringRepresentation = sb.ToString();
            }
            return _stringRepresentation;
        }
    }
}