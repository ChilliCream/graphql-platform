namespace ChilliCream.Nitro.CommandLine.Helpers;

internal interface IBrowserLauncher
{
    bool TryOpen(string url);
}
