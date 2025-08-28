using System.Globalization;

namespace HotChocolate.Types;

public class RequestMiddlewareTests
{
    [Fact]
    public async Task GenerateSource_RequestMiddleware_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                #nullable enable
                using System.Threading.Tasks;
                using HotChocolate;
                using HotChocolate.Execution;
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.DependencyInjection;

                public class Program
                {
                    public static void Main(string[] args)
                    {
                        var builder = WebApplication.CreateBuilder(args);
                        builder.Services
                            .AddGraphQLServer()
                            .UseRequest<SomeRequestMiddleware>();
                    }
                }

                public class SomeRequestMiddleware(
                    RequestDelegate next,
                    #pragma warning disable CS9113
                    [SchemaService] Service1 service1,
                    [SchemaService] Service2? service2)
                    #pragma warning restore CS9113
                {
                    public async ValueTask InvokeAsync(
                        IRequestContext context,
                        #pragma warning disable CS9113
                        Service1 service1,
                        Service2? service2)
                        #pragma warning restore CS9113
                    {
                        await next(context);
                    }
                }

                public class Service1;
                public class Service2;
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }
}
