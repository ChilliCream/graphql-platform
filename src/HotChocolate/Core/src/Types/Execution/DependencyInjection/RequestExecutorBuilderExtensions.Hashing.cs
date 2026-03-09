using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMD5DocumentHashProvider(
        this IRequestExecutorBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices(services =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new MD5DocumentHashProvider(format));
        });
    }

    public static IRequestExecutorBuilder AddSha1DocumentHashProvider(
        this IRequestExecutorBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices(services =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new Sha1DocumentHashProvider(format));
        });
    }

    public static IRequestExecutorBuilder AddSha256DocumentHashProvider(
        this IRequestExecutorBuilder builder,
        HashFormat format = HashFormat.Base64)
    {
        return builder.ConfigureSchemaServices(services =>
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(new Sha256DocumentHashProvider(format));
        });
    }
}
