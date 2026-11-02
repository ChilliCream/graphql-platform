namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed class SystemBrowserLauncher : IBrowserLauncher
{
    public bool TryOpen(string url) => SystemBrowser.TryOpen(url);
}
