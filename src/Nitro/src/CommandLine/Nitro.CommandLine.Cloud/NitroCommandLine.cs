using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

internal sealed class NitroCommandLine : CommandLineBuilder
{
    public NitroCommandLine() : base(new NitroRootCommand())
    {
        this.AddConsole()
            .AddService<IConfigurationService, ConfigurationService>()
            .AddSession()
            .AddResult()
            .AddApiClient()
            .UseDefaults()
            .AddMiddleware(ExceptionMiddleware, MiddlewareOrder.ExceptionHandler)
            .AddSessionMiddleware()
            .AddResultMiddleware();
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

            context.BindingContext.GetRequiredService<IAnsiConsole>().Error(message);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
        }
        catch (Exception exception)
        {
            context.ExitCode = ExitCodes.Error;
            context.BindingContext.GetRequiredService<IAnsiConsole>().ErrorLine(exception.Message);
            throw;
        }
    }
}
