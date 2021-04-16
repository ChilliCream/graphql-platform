using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.LanguageServer.Client;

namespace StrawberryShake.VisualStudio.Utilities
{
    internal static class ConnectionFactory
    {
        public static Connection CreateConnection(
            this Process languageServerProcess,
            string rootDirectory,
            bool enableLogging)
        {
            if (enableLogging)
            {
                return new Connection(
                    new TeeStream(languageServerProcess.StandardOutput.BaseStream, Path.Combine(rootDirectory, "out.languageServer.txt")),
                    new TeeStream(languageServerProcess.StandardInput.BaseStream, Path.Combine(rootDirectory, "in.languageServer.txt")));
            }

            return new Connection(
                languageServerProcess.StandardOutput.BaseStream,
                languageServerProcess.StandardInput.BaseStream);
        }
    }
}
