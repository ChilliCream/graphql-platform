using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Types;

public class SomeRequestMiddleware(RequestDelegate next, Service1 service1, Service2 service2)
{
    public async ValueTask InvokeAsync(IRequestContext context, Service3 service3)
    {
        await next(context);

        context.Result =
            OperationResultBuilder.New()
                .SetData(
                    new Dictionary<string, object?>
                    {
                        {
                            $"{service1.Say()} {service3.Hello()} {service2.World()}", true
                        },
                    })
                .Build();
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
