using System;
using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Console
{
    public class JsonConsoleOutput
        : IConsoleOutput
    {
        private readonly JsonConsoleOutputData _data =
            new JsonConsoleOutputData();

        public bool HasErrors { get; private set; }

        public IDisposable WriteCommand()
        {
            return new JsonConsoleOutputCommand(_data);
        }

        public IActivity WriteActivity(string text, string? path = null)
        {
            return new JsonConsoleOutputActivity(_data, text, path, () => HasErrors = true);
        }

        public void WriteFileCreated(string fileName)
        {
            _data.CreatedFiles.Add(fileName);
        }
    }
}
