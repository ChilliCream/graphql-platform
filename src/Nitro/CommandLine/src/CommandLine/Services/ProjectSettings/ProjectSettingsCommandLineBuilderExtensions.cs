using System.CommandLine.Builder;
using System.CommandLine.Invocation;

namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

internal static class ProjectSettingsCommandLineBuilderExtensions
{
    public static CommandLineBuilder AddProjectSettings(this CommandLineBuilder builder)
        => builder.AddService<IProjectSettingsService, ProjectSettingsService>();

    public static CommandLineBuilder AddProjectSettingsMiddleware(this CommandLineBuilder builder)
        => builder.AddMiddleware(InitializeProjectContext);

    private static async Task InitializeProjectContext(
        InvocationContext context,
        Func<InvocationContext, Task> next)
    {
        var service = context.BindingContext
            .GetService<IProjectSettingsService>();

        if (service is not null)
        {
            var cwd = Directory.GetCurrentDirectory();
            var settings = await service.LoadAsync(
                cwd, context.GetCancellationToken());

            if (settings is not null)
            {
                var settingsRoot = service.FindSettingsDirectory(cwd)!;
                var projectContext = service.ResolveContext(
                    settings, settingsRoot, cwd);
                context.BindingContext.AddService(_ => projectContext);
            }
        }

        await next(context);
    }
}
