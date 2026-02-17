using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

public static class CommandLineBuilderExtensions
{
    public static CommandLineBuilder AddService<T, TImpl>(this CommandLineBuilder builder)
        where TImpl : T, new()
    {
        T? value = default;

        builder.AddService(_ => value ??= new TImpl());

        return builder;
    }

    public static CommandLineBuilder AddService<T>(this CommandLineBuilder builder, T instance)
    {
        builder.AddService(_ => instance);
        return builder;
    }

    public static CommandLineBuilder AddService<T>(
        this CommandLineBuilder builder,
        Func<IServiceProvider, T> factory)
    {
        builder.AddMiddleware(x =>
        {
            var cache = default(T);
            x.BindingContext.AddService(sp => cache ??= factory(sp));
        }, MiddlewareOrder.Configuration);
        return builder;
    }

    public static CommandLineBuilder UseExceptionMiddleware(this CommandLineBuilder builder)
    {
        return builder.AddMiddleware(ExceptionMiddleware, MiddlewareOrder.ExceptionHandler);
    }

    public static CommandLineBuilder UseExtendedConsole(this CommandLineBuilder builder)
    {
        return builder.AddMiddleware(ExtendedConsoleMiddleware, MiddlewareOrder.Configuration)
            .AddService<IAnsiConsole>(sp => sp.GetRequiredService<ExtendedConsole>());
    }

    private static async Task ExtendedConsoleMiddleware(
        InvocationContext context,
        Func<InvocationContext, Task> next)
    {
        if (context.Console is not IAnsiConsole ansiConsole)
        {
            ansiConsole = AnsiConsole.Console;
        }

        var customConsole = ExtendedConsole.Create(ansiConsole);
        context.BindingContext.AddService(_ => customConsole);

        await next(context);
    }

    private static async Task ExceptionMiddleware(
        InvocationContext context,
        Func<InvocationContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch (ExitException exception) when (exception is { Message: var message })
        {
            context.ExitCode = ExitCodes.Error;

            context.BindingContext.GetRequiredService<IAnsiConsole>().MarkupLine(message);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            context.ExitCode = ExitCodes.Error;
            context.BindingContext.GetRequiredService<IAnsiConsole>().WriteLine(exception.Message);
            throw;
        }
    }
}
