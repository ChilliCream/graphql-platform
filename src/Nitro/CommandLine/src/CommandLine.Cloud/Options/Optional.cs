namespace ChilliCream.Nitro.CLI.Option;

internal static class Opt<TOption> where TOption : new()
{
    private static readonly object _lock = new();

    private static TOption? _instance;

    public static TOption Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new();
                }
            }

            return _instance;
        }
    }
}
