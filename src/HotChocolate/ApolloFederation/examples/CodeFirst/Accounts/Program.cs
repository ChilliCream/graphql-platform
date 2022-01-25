using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<UserRepository>();

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType<QueryType>()
    .RegisterService<UserRepository>()
    .AddDiagnosticEventListener<Log>();

var app = builder.Build();
app.MapGraphQL();
app.Run();

public class Log : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        Console.WriteLine(context.Request.Query?.ToString());

        if (context.Request.VariableValues is not null)
        {
            foreach (var variable in context.Request.VariableValues)
            {
                Console.WriteLine($"{variable.Key}: {((IValueNode)variable.Value!).ToString()}");
            }
        }

        return base.ExecuteRequest(context);
    }
}
