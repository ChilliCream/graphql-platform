namespace ChilliCream.Nitro.CommandLine.Options;

internal static class OptionExtensions
{
    public static Option<string> NonEmptyStringsOnly(
        this Option<string> option)
    {
        option.AddValidator(result =>
        {
            var value = result.GetValueForOption(option);
            if (value is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                var optionName = option.Name.StartsWith("--", StringComparison.Ordinal)
                    ? option.Name
                    : $"--{option.Name}";

                result.ErrorMessage = $"Expected a non-empty value for '{optionName}'.";
            }
        });

        return option;
    }

    public static Option<string> DefaultFileFromEnvironmentValue(
        this Option<string> option,
        string name)
    {
        option.DefaultFromEnvironmentValue(name);
        return option;
    }

    private const string Prefix = "NITRO_";
    private static readonly string[] s_prefixes = [Prefix, "BARISTA_"];

    public static Option<T> DefaultFromEnvironmentValue<T>(
        this Option<T> option,
        string name,
        Func<string, object>? transform = null,
        T? defaultValue = default)
    {
        var value = s_prefixes
            .Select(prefix => Environment.GetEnvironmentVariable(prefix + name))
            .FirstOrDefault(value => value is not null);

        if (value is not null)
        {
            transform ??= x => x;
            option.SetDefaultValueFactory(() => transform(value) ?? defaultValue);
        }
        else if (defaultValue is not null)
        {
            option.SetDefaultValue(defaultValue);
        }

        option.Description ??= "";
        option.Description = $"{option.Description} [env: {Prefix}{name}]";

        return option;
    }
}
