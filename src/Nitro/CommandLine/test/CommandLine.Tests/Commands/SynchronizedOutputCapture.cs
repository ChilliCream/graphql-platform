namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

internal sealed class SynchronizedOutputCapture
{
    private readonly StringWriter _output = new();
    private readonly TextWriter _writer;

    public SynchronizedOutputCapture()
    {
        _writer = TextWriter.Synchronized(_output);
    }

    public TextWriter Writer => _writer;

    public string GetOutput()
    {
        lock (_writer)
        {
            return _output.ToString();
        }
    }
}
