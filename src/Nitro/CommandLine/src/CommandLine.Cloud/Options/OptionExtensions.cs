namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal static class OptionExtensions
{
    public static Option<FileInfo> DefaultFileFromEnvironmentValue(
        this Option<FileInfo> option,
        string name)
    {
        option.DefaultFromEnvironmentValue(name, path => new FileInfo(path));
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
