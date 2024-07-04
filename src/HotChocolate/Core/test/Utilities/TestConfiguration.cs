using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Tests;

public class TestConfiguration
{
    public Action<IRequestExecutorBuilder>? Configure { get; set; }

    public Action<OperationRequestBuilder>? ConfigureRequest { get; set; }

    public IServiceProvider? Services { get; set; }
}
