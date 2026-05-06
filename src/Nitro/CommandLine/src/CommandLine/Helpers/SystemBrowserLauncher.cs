namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed class SystemBrowserLauncher : IBrowserLauncher
{
    public void Open(string url) => SystemBrowser.Open(url);
}
