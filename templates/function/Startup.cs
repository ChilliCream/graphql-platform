[assembly: FunctionsStartup(typeof(Startup))]

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder
            .AddGraphQLFunction()
            .AddQueryType<Query>();
    }
}
