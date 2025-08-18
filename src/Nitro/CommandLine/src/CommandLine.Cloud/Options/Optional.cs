namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal static class Opt<TOption> where TOption : new()
{
    private static readonly object s_lock = new();

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
