using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal static class SessionCommandLineBuilderExtensions
{
    public static CommandLineBuilder AddSession(this CommandLineBuilder builder)
        => builder.AddService(Factory);

    public static CommandLineBuilder AddSessionMiddleware(this CommandLineBuilder builder)
        => builder.AddMiddleware(InitializeService);

    private static async Task InitializeService(
        InvocationContext context,
        Func<InvocationContext, Task> next)
    {
        await context.BindingContext
            .GetRequiredService<ISessionService>()
            .LoadSessionAsync(context.GetCancellationToken());

        await next(context);
    }

    private static ISessionService Factory(IServiceProvider services)
    {
        return new SessionService(services.GetRequiredService<IConfigurationService>());
    }
}
