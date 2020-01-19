using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.VisualStudio.GUI
{
    internal class CreateClientViewModel
    {
        public string ServerUrl { get; set; }

        public string SchemaFile { get; set; }


        public void Ok()
        {
            
        }

        /*
        public override async Task<int> ExecuteAsync(
            InitCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new InitCommandContext(
                arguments.Schema.Value()?.Trim() ?? "schema",
                FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
                new Uri(arguments.Uri.Value!),
                accessToken?.Token,
                accessToken?.Scheme);

            FileSystem.EnsureDirectoryExists(context.Path);

            if (await DownloadSchemaAsync(context, cancellationToken).ConfigureAwait(false))
            {
                await WriteConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
                return 0;
            }

            return 1;
        }

        private async Task<bool> DownloadSchemaAsync(
            CancellationToken cancellationToken)
        {
            string schemaFilePath = Path.CombinePath(
                context.Path, context.SchemaFileName);

            HttpClient client = HttpClientFactory.Create(
                context.Uri, context.Token, context.Scheme);

            return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WriteConfigurationAsync(
           InitCommandContext context,
           CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Client configuration");

            Configuration configuration = ConfigurationStore.New();

            configuration.ClientName = context.ClientName;
            configuration.Schemas.Add(new SchemaFile
            {
                Type = "http",
                Name = context.SchemaName,
                File = context.SchemaFileName,
                Url = context.Uri.ToString()
            });

            await ConfigurationStore.SaveAsync(context.Path, configuration).ConfigureAwait(false);
        }
        */
    }
}
