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
        var console = context.BindingContext.GetRequiredService<INitroConsole>();
        var outputFormatOption = context.ParseResult.FindResultFor(Opt<OutputFormatOption>.Instance);
        var format = outputFormatOption?.GetValueOrDefault<OutputFormat>();

        // TODO: Maybe we have to wrap this as non-interactive
        await next(context);

        if (resultHolder.Result is { } result)
        {
            format ??= OutputFormat.Json;

            foreach (var formatter in formatters)
            {
                if (formatter.CanHandle(format.Value))
                {
                    formatter.Format(result);
                    break;
                }
            }
        }
        else if (format is OutputFormat.Json && context.ExitCode == 0)
        {
            console.WriteLine("{}");
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
            x => [new JsonResultFormatter(x.GetRequiredService<INitroConsole>())]);
    }

    private sealed class ResultHolder
    {
        public Result? Result { get; set; }
    }
}
