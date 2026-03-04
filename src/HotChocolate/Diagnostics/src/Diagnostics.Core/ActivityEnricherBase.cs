using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

/// <summary>
/// Base class for activity enrichers that provides shared enrichment logic
/// for HTTP request handling, error handling, and common span enrichment.
/// </summary>
public abstract class ActivityEnricherBase(InstrumentationOptionsBase options)
{
}
