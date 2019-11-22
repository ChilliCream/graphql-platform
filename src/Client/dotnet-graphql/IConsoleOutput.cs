namespace StrawberryShake.Tools
{
    public interface IConsoleOutput
    {
        IActivity WriteActivity(string text);
        void WriteFileCreated(string fileName);
    }
}
