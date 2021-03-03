using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class NoOpLogger : ILogger
    {
        public void SetLocation(string location)
        {
        }

        public void Begin(GraphQLConfig config, ClientGeneratorContext context)
        {
        }

        public void ClientDocuments(IReadOnlyList<string> documents)
        {
        }

        public void BeginGenerateCode()
        {
        }

        public void EndGenerateCode()
        {
        }

        public void WriteDocument(string documentName)
        {
        }

        public void BeginClean()
        {
        }

        public void RemoveFile(string fileName)
        {
        }

        public void EndClean()
        {
        }

        public void Error(Exception exception)
        {
        }

        public void End()
        {
        }

        public void Dispose()
        {
        }
    }
}
