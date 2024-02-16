using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace HotChocolate.Types;

[ExtendObjectType(OperationType.Query)]
public static class StaticQueryExtension
{
    public static string StaticField() => "foo";
}

public class SomeRequestMiddleware
{
    private readonly RequestDelegate _next;

    public SomeRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public ValueTask InvokeAsync(IRequestContext context)
    {
        return _next(context);
    }
}