using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChilliCream.Hosting.CommandLine;

public static class HostCommandRunner
{
    /// <inheritdoc cref="RunAsync"/>
    public static int Run(IHost host, string[] args, Action<RootCommand, IServiceProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configure);

        try
        {
            if (args.Length > 0)
            {
                using var scope = host.Services.CreateScope();

                if (ParseCommand(scope.ServiceProvider, args, configure) is { } command)
                {
                    return command.Invoke();
                }
            }

            host.Start();
            host.WaitForShutdown();
        }
        finally
        {
            host.Dispose();
        }

        return Environment.ExitCode;
    }

    /// <summary>
    /// Runs the selected command or the host, then disposes the host.
    /// </summary>
    /// <returns>
    /// The command's exit code or <see cref="Environment.ExitCode"/> after host shutdown.
    /// </returns>
    public static async Task<int> RunAsync(
        IHost host,
        string[] args,
        Action<RootCommand, IServiceProvider> configure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configure);

        try
        {
            if (args.Length > 0)
            {
                await using var scope = host.Services.CreateAsyncScope();

                if (ParseCommand(scope.ServiceProvider, args, configure) is { } command)
                {
                    return await command.InvokeAsync(cancellationToken: cancellationToken);
                }
            }

            await host.StartAsync(cancellationToken);
            await host.WaitForShutdownAsync(cancellationToken);
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }

        return Environment.ExitCode;
    }

    private static ParseResult? ParseCommand(
        IServiceProvider services,
        string[] args,
        Action<RootCommand, IServiceProvider> configure)
    {
        var root = new RootCommand("Run the application or one of its commands.");

        configure(root, services);

        foreach (var contributor in services.GetServices<IHostCommandContributor>())
        {
            contributor.Configure(root);
        }

        root.SetAction(_ => { });
        var result = root.Parse(args);
        if (result.Errors.Count > 0
            && result.UnmatchedTokens.Count > 0
            && TryRemoveHostSettings(root, args, out var commandArgs))
        {
            var options = root.Parse(commandArgs);
            if (options.Action == root.Action || options.Errors.Count > 0)
            {
                result = options;
            }
        }

        return result.Action == root.Action ? null : result;
    }

    private static bool TryRemoveHostSettings(RootCommand root, string[] args, out List<string> commandArgs)
    {
        commandArgs = [];

        for (var i = 0; i < args.Length; i++)
        {
            var start = i;
            var arg = args[i];
            var prefixLength = arg switch
            {
                ['-', '-', ..] => 2,
                ['/', ..] => 1,
                _ => 0
            };

            var separator = arg.IndexOf('=');
            if (arg.Length == prefixLength
                || separator == prefixLength
                || (prefixLength == 0 && arg[0] == '-'))
            {
                return false;
            }

            if (separator < 0 && (prefixLength == 0 || ++i == args.Length))
            {
                return false;
            }

            if (IsRootOption(root, arg) || (start != i && IsRootOption(root, args[i])))
            {
                commandArgs.AddRange(args.AsSpan(start, i - start + 1));
            }
        }

        return commandArgs.Count < args.Length;
    }

    private static bool IsRootOption(RootCommand root, string argument)
    {
        var separator = argument.AsSpan().IndexOfAny(':', '=');
        var name = separator < 0 ? argument : argument[..separator];
        foreach (var option in root.Options)
        {
            if (option.Name == name || option.Aliases.Contains(name)
                || (separator >= 0 && (option.Name == argument || option.Aliases.Contains(argument))))
            {
                return true;
            }
        }

        return false;
    }
}
