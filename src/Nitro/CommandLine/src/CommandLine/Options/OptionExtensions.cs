using ChilliCream.Nitro.CommandLine.Helpers;

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
        T? defaultValue = default)
    {
        option.DefaultValueFactory = _ =>
        {
            var provider = CommandExecutionContext.Services.Value!
                .GetRequiredService<IEnvironmentVariableProvider>();
            var value = s_prefixes
                .Select(prefix => provider.GetEnvironmentVariable(prefix + name))
                .FirstOrDefault(value => value is not null);

            if (value is not null)
            {
                return ConvertEnvironmentValue<T>(value);
            }

            return defaultValue!;
        };

        option.Description ??= "";
        option.Description = $"{option.Description} [env: {Prefix}{name}]";

        return option;
    }

    private static T ConvertEnvironmentValue<T>(string value)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }

        if (targetType.IsEnum)
        {
            return (T)Enum.Parse(targetType, value, ignoreCase: true);
        }

        return (T)Convert.ChangeType(value, targetType);
    }
}
