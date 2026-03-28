namespace ChilliCream.Nitro.CommandLine;

internal static class ArgumentExtensions
{
    public static void LegalFilePathsOnly<T>(this Argument<T> argument)
    {
        argument.Validators.Add(result =>
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
