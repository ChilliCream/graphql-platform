using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CookieCrumble.Xunit;

[XunitTestCaseDiscoverer("LocalFactDiscoverer", "YourTestAssemblyName")]
public class LocalFactAttribute : FactAttribute;

public class LocalFactDiscoverer(IMessageSink diagnosticMessageSink) : FactDiscoverer(diagnosticMessageSink)
{
    private readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
    {
        if (TestEnvironment.IsLocalEnvironment())
        {
            return new XunitTestCase(
                _diagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod);
        }

        return new ExecutionErrorTestCase(
            _diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            "LocalFact tests cannot run in CI environment");
    }
}
