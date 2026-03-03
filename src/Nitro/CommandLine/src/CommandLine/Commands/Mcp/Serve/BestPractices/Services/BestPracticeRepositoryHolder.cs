namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

internal static class BestPracticeRepositoryHolder
{
    private static readonly Lazy<BestPracticeRepository> _instance = new(() => new BestPracticeRepository());

    public static BestPracticeRepository Instance => _instance.Value;
}
