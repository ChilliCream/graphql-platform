using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public class PolicyResultContext<T>
{
    public PolicyResultContext(string policyPath, T result, AuthorizationContext context)
    {
        PolicyPath = policyPath;
        Result = result;
        Context = context;
    }

    public string PolicyPath { get; }

    public T? Result { get; }

    public AuthorizationContext Context { get; }
}
