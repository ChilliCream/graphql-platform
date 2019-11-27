using System;

namespace StrawberryShake.Tools
{
    public class JsonConsoleOutput
        : IConsoleOutput
    {
        private readonly JsonConsoleOutputData _data =
            new JsonConsoleOutputData();

        public IDisposable WriteCommand()
        {
            return new JsonConsoleOutputCommand(_data);
        }

        public IActivity WriteActivity(string text, string? path = null)
        {
            return new JsonConsoleOutputActivity(_data, text, path);
        }

        public void WriteFileCreated(string fileName)
        {
            _data.CreatedFiles.Add(fileName);
        }
    }
}
