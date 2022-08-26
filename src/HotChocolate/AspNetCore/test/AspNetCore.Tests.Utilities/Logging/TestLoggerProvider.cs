using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities.Logging;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILoggerFactory _loggerFactory;
#if NET5_0_OR_GREATER
    private readonly ConsoleFormatter _loggingFormatter;
#endif

    private readonly ServiceProvider? _loggingServiceProvider;

    public TestLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

#if NET5_0_OR_GREATER
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Disabled;
                options.IncludeScopes = true;
                options.SingleLine = false;
                options.TimestampFormat = "hh:mm:ss ";
            });
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _loggingServiceProvider = serviceProvider;
        _loggingFormatter = serviceProvider.GetRequiredService<ConsoleFormatter>();
#endif
    }

#if NET5_0_OR_GREATER
    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(categoryName, _testOutputHelper, _loggingFormatter);
    }
#else
    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(categoryName, _testOutputHelper);
    }
#endif

    public void Dispose()
    {
        _loggingServiceProvider?.Dispose();
    }
}
