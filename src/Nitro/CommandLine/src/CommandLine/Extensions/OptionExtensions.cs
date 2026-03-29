using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine;

internal static class OptionExtensions
{
    public static void LegalFilePathsOnly(this Option<string> option)
    {
        option.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();

           ValidateFilePath(result, value);
        });
    }

    public static void LegalFilePathsOnly(this Option<List<string>> option)
    {
        option.Validators.Add(result =>
        {
            var values = result.GetValueOrDefault<List<string>>();

            values.ForEach(value => ValidateFilePath(result, value));
        });
    }

    private static void ValidateFilePath(OptionResult result, string value)
    {
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
    }
}
