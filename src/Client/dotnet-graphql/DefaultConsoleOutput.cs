using System;

namespace StrawberryShake.Tools
{
    public class DefaultConsoleOutput
        : IConsoleOutput
    {
        public IDisposable WriteCommand() => new DummyConsoleContext();

        public IActivity WriteActivity(string text, string? path = null)
            => new DefaultConsoleOutputActivity(text, path);

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
