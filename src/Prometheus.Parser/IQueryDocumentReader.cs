using System.Collections.Generic;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public interface IQueryDocumentReader
    {
        IQueryDocument Read(string query);
    }
}