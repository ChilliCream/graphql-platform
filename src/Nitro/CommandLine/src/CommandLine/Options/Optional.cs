namespace ChilliCream.Nitro.CommandLine.Options;

internal static class Opt<TOption> where TOption : new()
{
#if NET9_0_OR_GREATER
    private static readonly Lock s_lock = new();
#else
    private static readonly object s_lock = new();
#endif

    private static TOption? s_instance;

    public static TOption Instance
    {
        get
        {
            if (s_instance == null)
            {
                lock (s_lock)
                {
                    s_instance ??= new();
                }
            }

            return s_instance;
        }
    }
}
