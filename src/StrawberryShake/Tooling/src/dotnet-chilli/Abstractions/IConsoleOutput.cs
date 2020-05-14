using System;

namespace StrawberryShake.Tools.Abstractions
{
    public interface IConsoleOutput
    {
        bool HasErrors { get; }

        IDisposable WriteCommand();

        IActivity WriteActivity(string text, string? path = null);

        void WriteFileCreated(string fileName);
    }
}
