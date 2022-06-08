using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

public class PolicyResultContext<T>
{
    public PolicyResultContext(string policyPath, T result, IMiddlewareContext context)
    {
        PolicyPath = policyPath;
        Result = result;
        MiddlewareContext = context;
    }

    public string PolicyPath { get; }
    public T? Result { get; }
    public IMiddlewareContext MiddlewareContext { get; }
}
