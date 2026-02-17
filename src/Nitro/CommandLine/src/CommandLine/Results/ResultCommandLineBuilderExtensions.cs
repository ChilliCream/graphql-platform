using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Results;

internal static class ResultCommandLineBuilderExtensions
{
    public static CommandLineBuilder AddResult(this CommandLineBuilder builder)
        => builder
            .AddResultFormatters()
            .AddService(new ResultHolder());

    public static CommandLineBuilder AddResultMiddleware(this CommandLineBuilder builder)
        => builder.AddMiddleware(Middleware);

    private static async Task Middleware(
        InvocationContext context,
        Func<InvocationContext, Task> next)
    {
        var resultHolder = context.BindingContext.GetRequiredService<ResultHolder>();
        var formatters = context.BindingContext.GetServices<IResultFormatter>();
        var format =
            context.ParseResult.FindResultFor(Opt<OutputFormatOption>.Instance)?.GetValueOrDefault<OutputFormat>();
        var isInteractive = format is null;
        var extendedConsole =
            context.BindingContext.GetRequiredService<IAnsiConsole>() as IExtendedConsole ??
            context.BindingContext.GetRequiredService<ExtendedConsole>();

        extendedConsole.IsInteractive = isInteractive;

        format ??= OutputFormat.Json;

        try
        {
            await next(context);
        }
        finally
        {
            extendedConsole.IsInteractive = true;
        }

        if (resultHolder.Result is { } result)
        {
            foreach (var formatter in formatters)
            {
                if (formatter.CanHandle(format.Value))
                {
                    formatter.Format(result);
                    break;
                }
            }
        }
    }

    public static void SetResult(this InvocationContext context, object result)
    {
        var resultHolder = context.BindingContext.GetRequiredService<ResultHolder>();
        resultHolder.Result = new ObjectResult(result);
    }

    public static CommandLineBuilder AddResultFormatters(
        this CommandLineBuilder builder)
    {
        return builder.AddService<IEnumerable<IResultFormatter>>(
            x => [new JsonResultFormatter(x.GetRequiredService<IAnsiConsole>())]);
    }

    private sealed class ResultHolder
    {
        public Result? Result { get; set; }
    }
}
