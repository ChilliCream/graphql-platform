using System;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CookieCrumble;

[XunitTestCaseDiscoverer("LocalFactDiscoverer", "YourTestAssemblyName")]
public class LocalFactAttribute : FactAttribute;

public class LocalFactDiscoverer : FactDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;

    public LocalFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }


    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        if (TestEnvironment.IsLocalEnvironment())
        {
            return new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
        }
        else
        {
            return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, "LocalFact tests cannot run in CI environment");
        }
    }
}
