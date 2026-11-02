namespace ChilliCream.Nitro.CommandLine;

internal static class ArgumentExtensions
{
    public static void LegalFilePathsOnly(this Argument<string> argument)
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
