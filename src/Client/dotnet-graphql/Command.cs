using System.IO;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using HCError = HotChocolate.IError;
using HCErrorBuilder = HotChocolate.ErrorBuilder;
using IOPath = System.IO.Path;

namespace StrawberryShake.Tools
{
    public abstract class CommandHandler<T>
    {
        public abstract Task<int> ExecuteAsync(T arguments);
    }

    public interface IHttpClientFactory
    {
        HttpClient Create(string uri, string? token, string? scheme);
    }

    public class DefaultHttpClientFactory
        : IHttpClientFactory
    {
        public HttpClient Create(string uri, string token, string scheme)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(uri);
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    new ProductHeaderValue(
                        "StrawberryShake",
                        typeof(InitCommand).Assembly!.GetName()!.Version!.ToString())));

            if (token is { })
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(scheme ?? "bearer", token);
            }
            return httpClient;
        }
    }

    public interface IFileSystem
    {
        string CurrentDirectory { get; }

        string ResolvePath(string? path);

        string CombinePath(params string[] path);

        Task WriteToAsync(string fileName, Func<Stream, Task> write);
    }

    public interface IConsoleOutput
    {
        IActivity WriteActivity(string text);
        void WriteFileCreated(string fileName);
    }

    public interface IActivity : IDisposable
    {
        void WriteError(HCError error);
    }

    public class InitCommandArguments
    {
        public InitCommandArguments(
            CommandArgument uri,
            CommandOption path,
            CommandOption schema,
            CommandOption token,
            CommandOption scheme)
        {
            Uri = uri;
            Path = path;
            Schema = schema;
            Token = token;
            Scheme = scheme;
        }

        public CommandArgument Uri { get; }
        public CommandOption Path { get; }
        public CommandOption Schema { get; }
        public CommandOption Token { get; }
        public CommandOption Scheme { get; }
    }

    public class InitCommandHandler
        : CommandHandler<InitCommandArguments>
    {
        public InitCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConsoleOutput output)
        {
            FileSystem = fileSystem;
            HttpClientFactory = httpClientFactory;
            Output = output;
        }

        public IFileSystem FileSystem { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public IConsoleOutput Output { get; }

        public override Task<int> ExecuteAsync(InitCommandArguments arguments)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string schemaName = (schemaArg.HasValue()
                ? schemaArg.Value()!
                : "schema").Trim();
            string schemaFielName = schemaName + ".graphql";
        }

        private async Task<bool> DownloadSchemaAsync(
            InitCommandArguments arguments,
            string schemaFielName,
            string path)
        {
            using var activity = Output.WriteActivity("Download schema");

            try
            {
                HttpClient client = HttpClientFactory.Create(
                    arguments.Uri.Value!,
                    arguments.Token.Value(),
                    arguments.Schema.Value());
                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(client);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                using (var stream = File.Create(IOPath.Combine(path, schemaFielName)))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        SchemaSyntaxSerializer.Serialize(schema, sw, true);
                    }
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                activity.WriteError(
                    HCErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetCode("HTTP_ERROR")
                        .Build());

                return false;
            }
        }
    }
}
