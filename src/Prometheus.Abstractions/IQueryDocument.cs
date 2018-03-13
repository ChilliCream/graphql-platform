using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Abstractions
{
    public interface IQueryDocument
        : IEnumerable<IQueryDefinition>
    {
        OperationDefinition GetOperation(string operationName);
        FragmentDefinition GetFragment(string fragmentName, NamedType type);
    }
}