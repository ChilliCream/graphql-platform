using System.Collections.Generic;
using Zeus.Abstractions;

namespace Zeus.Parser
{
    public interface IQueryDocumentReader
    {
        QueryDocument Read(string query);
    }
}