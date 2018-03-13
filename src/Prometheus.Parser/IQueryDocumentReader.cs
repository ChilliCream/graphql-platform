using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public interface IQueryDocumentReader
    {
        QueryDocument Read(string query);
    }
}