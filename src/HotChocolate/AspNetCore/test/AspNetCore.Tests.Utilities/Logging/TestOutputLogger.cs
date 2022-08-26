using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities.Logging;

public class TestOutputLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _name;

    internal IExternalScopeProvider? ScopeProvider { get; set; }

#if NET5_0_OR_GREATER
    private readonly ConsoleFormatter _formatter;
    public TestOutputLogger(string categoryName, ITestOutputHelper testOutputHelper, ConsoleFormatter formatter)
    {
        _categoryName = categoryName;
        _testOutputHelper = testOutputHelper;
        _formatter = formatter;
    }
#else
    public TestOutputLogger(string categoryName, ITestOutputHelper testOutputHelper)
    {
        _categoryName = categoryName;
        _testOutputHelper = testOutputHelper;
    }
#endif

    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

#if NET5_0_OR_GREATER
        var logEntry =
            new LogEntry<TState>(logLevel, _categoryName, eventId, state, exception, formatter);
        var outputWriter = new StringWriter();
        _formatter.Write(logEntry, default, outputWriter);
        _testOutputHelper.WriteLine(outputWriter.ToString());
#endif
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public IDisposable BeginScope<TState>(TState state) =>
        ScopeProvider?.Push(state) ?? NullScope.Instance;
}
