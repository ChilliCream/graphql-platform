namespace ChilliCream.Nitro.CommandLine;

internal static class OptionExtensions
{
    public static void LegalFilePathsOnly<T>(this Option<T> option)
    {
        option.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                Path.GetFullPath(value);
            }
            catch
            {
                result.AddError($"Invalid file path: '{value}'");
            }
        });
    }
}
