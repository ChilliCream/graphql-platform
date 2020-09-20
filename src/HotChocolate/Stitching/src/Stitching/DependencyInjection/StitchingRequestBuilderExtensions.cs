using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Configuration;
using HotChocolate;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StitchingRequestBuilderExtensions
    {
        public static IRequestExecutorBuilder AddRemoteRequestExecutor(
            this IRequestExecutorBuilder builder,
            NameString name,
            Func<IServiceProvider, IRequestExecutor> factory)
        {
            throw new Exception();
        }

        public static IRequestExecutorBuilder AddRemoteRequestExecutor(
            this IRequestExecutorBuilder builder,
            NameString name,
            Func<IServiceProvider, Task<IRequestExecutor>> factory)
        {
            throw new Exception();
        }

        public static IRequestExecutorBuilder AddRemoteSchema(
            this IRequestExecutorBuilder builder,
            NameString name,
            Func<IServiceProvider, DocumentNode> factory)
        {
            throw new Exception();
        }

        public static IRequestExecutorBuilder AddRemoteSchemaAsync(
            this IRequestExecutorBuilder builder,
            NameString name,
            Func<IServiceProvider, Task<DocumentNode>> factory)
        {
            throw new Exception();
        }

        public static IRequestExecutorBuilder AddRemoteSchemaFromHttp(
            this IRequestExecutorBuilder builder,
            NameString name)
        {
            throw new Exception();
        }

        public static IRequestExecutorBuilder AddRemoteSchemaRewriter(
            this IRequestExecutorBuilder builder,
            NameString name,
            Func<IServiceProvider, IDocumentRewriter> factory)
        {
            throw new Exception();
        }
    }

    public class Foo
    {

public void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL()
        .AddMicrosoftGraph()
        .AddTypeExtension<UserExtension>();
}

[ExtendObjectType(Name = "User")]
public class UserExtension
{
    public string Greetings([Parent("displayName")]string displayName)
    {
        return "hello " + displayName;
    }
}

    }
}
