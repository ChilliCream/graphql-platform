namespace Microsoft.Extensions.DependencyInjection
{
    public static class Foo
    {
        public static void Bar(IServiceCollection services)
        {
            services
                .AddGraphQL("Foo")
                .UseField(next => context => 
                {
                    return next(context);
                })
        }
    }
}