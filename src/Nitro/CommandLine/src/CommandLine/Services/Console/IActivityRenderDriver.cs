namespace ChilliCream.Nitro.CommandLine;

internal interface IActivityRenderDriver
{
    Task Completion { get; }

    void Stop();
}
