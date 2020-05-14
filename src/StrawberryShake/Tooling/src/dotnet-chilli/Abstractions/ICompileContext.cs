namespace StrawberryShake.Tools.Abstractions
{
    public interface ICompileContext
    {
        string Path { get; }
        bool Search { get; }
    }
}
