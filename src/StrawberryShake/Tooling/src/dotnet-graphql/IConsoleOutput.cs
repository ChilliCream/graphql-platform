namespace StrawberryShake.Tools;

public interface IConsoleOutput
{
    bool HasErrors { get; }

    IDisposable WriteCommand();

    IActivity WriteActivity(string text, string? path = null);

    void WriteFileCreated(string fileName);
}
