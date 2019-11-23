using System;

namespace StrawberryShake.Tools
{
    public interface IConsoleOutput
    {
        IDisposable WriteCommand();
        IActivity WriteActivity(string text);
        void WriteFileCreated(string fileName);
    }
}
