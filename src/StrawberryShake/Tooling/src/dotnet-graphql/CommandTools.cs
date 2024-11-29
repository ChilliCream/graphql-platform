using System.Text;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools;

public static class CommandTools
{
    public static void Write(this IEnumerable<HCError> errors)
    {
        var message = new StringBuilder();

        foreach (var error in errors)
        {
            Write(error, message);
        }
    }

    public static void Write(this HCError error)
    {
        Write(error, new StringBuilder());
    }

    public static void Write(this HCError error, StringBuilder message)
    {
        message.Clear();

        if (error.Extensions is { } && error.Extensions.ContainsKey("fileName"))
        {
            message.Append($"{IOPath.GetFullPath((string)error.Extensions["fileName"]!)}");
        }

        if (error.Locations is { } && error.Locations.Count > 0)
        {
            var location = error.Locations[0];
            message.Append($"({location.Line},{location.Column}): ");
        }
        message.Append($"error {error.Code ?? "GQL"}: {error.Message}");

        Console.WriteLine(message.ToString());
    }

    public static void AddHelp<T>(this CommandLineApplication app)
        where T : IHelpTextGenerator, new()
    {
        app.HelpOption("-h|--help");
        app.HelpTextGenerator = new T();
    }

    public static T CreateHandler<T>(CommandOption jsonArg) where T : class =>
        CreateHandler<T>(jsonArg.HasValue());

    public static T CreateHandler<T>(bool jsonOutput) where T : class
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
        services.AddSingleton<IFileSystem, DefaultFileSystem>();

        if (jsonOutput)
        {
            services.AddSingleton<IConsoleOutput, JsonConsoleOutput>();
        }
        else
        {
            services.AddSingleton<IConsoleOutput, DefaultConsoleOutput>();
        }

        services.AddSingleton<T>();
        return services.BuildServiceProvider().GetRequiredService<T>();
    }
}
