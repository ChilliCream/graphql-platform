namespace Mocha.Tests;

/// <summary>
/// Runs the tests that observe the process-wide Mocha activity source without other tests in parallel.
/// </summary>
[CollectionDefinition("OpenTelemetry", DisableParallelization = true)]
public sealed class OpenTelemetryCollection;
