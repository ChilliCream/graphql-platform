namespace StrawberryShake.Tools;

public interface ICompileContext
{
    string Path { get; }
    bool Search { get; }
}
