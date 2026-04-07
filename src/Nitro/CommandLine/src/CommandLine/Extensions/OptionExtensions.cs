using System.CommandLine.Parsing;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;

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

            foreach (var value in values ?? [])
            {
                ValidateFilePath(result, value);
            }
        });
    }

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
        option.DefaultValueFactory = r =>
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

        // DefaultValueFactory bypasses System.CommandLine's Required validation,
        // so we enforce it ourselves.
        option.Validators.Add(result =>
        {
            if (!option.Required)
            {
                return;
            }

            // Skip validation when a subcommand is being invoked,
            // since the parent's options don't apply to subcommands.
            if (result.Parent is CommandResult commandResult
                && commandResult.Children.OfType<CommandResult>().Any())
            {
                return;
            }

            var value = result.GetValue(option);
            if (value is null)
            {
                var optionName = option.Name.StartsWith("--", StringComparison.Ordinal)
                    ? option.Name
                    : $"--{option.Name}";

                result.AddError($"Option '{optionName}' is required.");
            }
        });

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
