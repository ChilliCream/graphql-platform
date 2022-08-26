using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities.Logging;

public class TestLoggingBuilder : ILoggingBuilder
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestLoggingBuilder(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public IServiceCollection Services { get; }
}