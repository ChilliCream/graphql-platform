namespace GreenDonut;

internal sealed class NoopDataLoaderDiagnosticEventListener : DataLoaderDiagnosticEventListener
{
    internal static readonly NoopDataLoaderDiagnosticEventListener Default = new();
}
