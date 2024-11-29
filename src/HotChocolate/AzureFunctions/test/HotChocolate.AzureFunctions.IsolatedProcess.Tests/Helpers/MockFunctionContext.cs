using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

/// <summary>
/// Created Based on original (internal) class form Microsoft here:
/// https://github.com/Azure/azure-functions-dotnet-worker/blob/
/// 7ffd5c48a08b6b95a7b2e5826105e39c49194a23/src/DotNetWorker.Core/Context/DefaultFunctionContext.cs
/// </summary>
public class MockFunctionContext : FunctionContext, IDisposable
{
    public MockFunctionContext(
        IServiceProvider serviceProvider,
        FunctionDefinition? functionDefinition= null,
        IInvocationFeatures? features = null,
        TraceContext? traceContext = null,
        BindingContext? bindingContext = null,
        RetryContext? retryContext = null
    )
    {
        InstanceServices = serviceProvider;
        FunctionDefinition = functionDefinition ?? new MockFunctionDefinition();
        Features = features ?? new MockInvocationFeatures();
        TraceContext = traceContext ?? new MockTraceContext("Root", "Ok");
        BindingContext = bindingContext ?? new MockBindingContext();
        RetryContext = retryContext ?? new MockRetryContext();
    }

    public override string InvocationId => nameof(MockFunctionContext);

    public override string FunctionId
        => string.Concat(nameof(MockFunctionContext), "-", Guid.NewGuid());

    public override FunctionDefinition FunctionDefinition { get; }

    public override IDictionary<object, object> Items { get; set; } =
        new Dictionary<object, object>();

    public override IInvocationFeatures Features { get; }

    public override IServiceProvider InstanceServices { get; set; }

    public override TraceContext TraceContext { get; }

    public override BindingContext BindingContext { get; }

    public override RetryContext RetryContext { get; }

    public virtual void Dispose()
    {
    }
}

public class MockTraceContext : TraceContext
{
    public MockTraceContext(string traceParent, string traceState)
    {
        TraceParent = traceParent;
        TraceState = traceState;
    }
    public override string TraceParent { get; }
    public override string TraceState { get; }
}

public class MockBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object?> BindingData { get; } =
        new Dictionary<string, object?>();
}

public class MockRetryContext : RetryContext
{
    public MockRetryContext(int retryCount = 0, int maxRetryCount = 0)
    {
        RetryCount = retryCount;
        MaxRetryCount = maxRetryCount;
    }
    public override int RetryCount { get; }
    public override int MaxRetryCount { get; }
}

public class MockInvocationFeatures : Dictionary<Type, object>, IInvocationFeatures
{
    public void Set<T>(T instance)
    {
        if (instance == null)
        {
            return;
        }

        TryAdd(typeof(T), instance);
    }

    public T? Get<T>()
    {
        return TryGetValue(typeof(T), out var result)
            ? (T)result
            : default;
    }
}

public class MockFunctionDefinition : FunctionDefinition
{
    public MockFunctionDefinition()
    { }

    public MockFunctionDefinition(
        string id,
        string name,
        string pathToAssembly,
        string entryPoint,
        ImmutableArray<FunctionParameter> parameters,
        IImmutableDictionary<string, BindingMetadata> inputBindings,
        IImmutableDictionary<string, BindingMetadata> outputBindings
    )
    {
        Id = id;
        Name = name;
        PathToAssembly = pathToAssembly;
        EntryPoint = entryPoint;
        Parameters = parameters;
        InputBindings = inputBindings;
        OutputBindings = outputBindings;
    }

    public override string Id { get; } = string.Empty;
    public override string Name { get; } = string.Empty;
    public override string PathToAssembly { get; } = string.Empty;
    public override string EntryPoint { get; } = string.Empty;
    public override ImmutableArray<FunctionParameter> Parameters { get; } = [];
    public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } =
        ImmutableDictionary<string, BindingMetadata>.Empty;
    public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } =
        ImmutableDictionary<string, BindingMetadata>.Empty;
}
