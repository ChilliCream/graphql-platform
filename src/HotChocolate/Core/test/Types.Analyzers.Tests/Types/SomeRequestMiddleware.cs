using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Types;

public class SomeRequestMiddleware(RequestDelegate next, Service1 service1, Service2 service2)
{
    public async ValueTask InvokeAsync(RequestContext context, Service3 service3)
    {
        await next(context);

        var builder = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
        builder.Add("middleware", $"{service1.Say()} {service3.Hello()} {service2.World()}");

        context.Result = new OperationResult(builder.ToImmutable());
    }
}

public class Service1
{
    public string Say() => nameof(Say);
}

public class Service2
{
    public string World() => nameof(World);
}

public class Service3
{
    public string Hello() => nameof(Hello);
}
