namespace ChilliCream.Nitro.CommandLine.Options;

internal static class OptionExtensions
{
    public static Option<string> NonEmptyStringsOnly(
        this Option<string> option)
    {
        option.Validators.Add(result =>
        {
            var value = result.GetValue(option);
            if (value is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                var optionName = option.Name.StartsWith("--", StringComparison.Ordinal)
                    ? option.Name
                    : $"--{option.Name}";

                result.AddError($"Expected a non-empty value for '{optionName}'.");
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
        Func<string, T>? transform = null,
        T? defaultValue = default)
    {
        var value = s_prefixes
            .Select(prefix => Environment.GetEnvironmentVariable(prefix + name))
            .FirstOrDefault(value => value is not null);

        if (value is not null)
        {
            // TODO: How to fix this
            // transform ??= x => x;
            // option.DefaultValueFactory = result => transform(value) ?? defaultValue;
        }
        else if (defaultValue is not null)
        {
            option.DefaultValueFactory = _ => defaultValue;
        }

        option.Description ??= "";
        option.Description = $"{option.Description} [env: {Prefix}{name}]";

        return option;
    }
}
