using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public interface ILogger : IDisposable
    {
        void Begin(GraphQLConfig config, ClientGeneratorContext context);

        void ClientDocuments(IReadOnlyList<string> documents);

        void BeginGenerateCode();

        void EndGenerateCode();

        void WriteDocument(string documentName);

        void BeginClean();

        void RemoveFile(string fileName);

        void EndClean();

        void Error(Exception exception);

        void End();
    }
}
