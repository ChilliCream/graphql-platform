using System;
using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Console
{
    public class DefaultConsoleOutput
        : IConsoleOutput
    {
        public bool HasErrors { get; private set; }

        public IDisposable WriteCommand() => new DummyConsoleContext();

        public IActivity WriteActivity(string text, string? path = null)
            => new DefaultConsoleOutputActivity(text, path, () => HasErrors = true);

        public void WriteFileCreated(string fileName)
        {
        }

        private class DummyConsoleContext : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
