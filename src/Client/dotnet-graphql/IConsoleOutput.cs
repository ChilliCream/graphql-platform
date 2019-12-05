using System;

namespace StrawberryShake.Tools
{
    public interface IConsoleOutput
    {
        IDisposable WriteCommand();

        IActivity WriteActivity(string text, string? path = null);

        void WriteFileCreated(string fileName);
    }
}
