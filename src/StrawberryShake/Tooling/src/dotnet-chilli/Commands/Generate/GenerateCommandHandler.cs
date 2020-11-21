using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Commands.Compile;
using StrawberryShake.Tools.Config;
using HCError = HotChocolate.IError;
using IHttpClientFactory = StrawberryShake.Tools.Abstractions.IHttpClientFactory;

namespace StrawberryShake.Tools.Commands.Generate
{
    public class GenerateCommandHandler : CompileCommandHandlerBase<Options.Generate, GenerateCommandContext>
    {
        private readonly IFileSystem FileSystem;
        private readonly IHttpClientFactory HttpClientFactory;
        private readonly IConfigurationStore Configuration;
        private readonly IConsoleOutput ConsoleOutput;

        public GenerateCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output) : base(fileSystem, httpClientFactory, configurationStore, output)
        {
            FileSystem = fileSystem;
            HttpClientFactory = httpClientFactory;
            Configuration = configurationStore;
            ConsoleOutput = output;
        }

        protected override GenerateCommandContext CreateContext(Options.Generate arguments)
        {
            LanguageVersion languageVersion =
                (!(arguments.Language is { Length: > 0 }) || arguments.Language?.Trim() == "7.3")
                ? LanguageVersion.CSharp_7_3
                : LanguageVersion.CSharp_8_0;

            string path = arguments.Path?.Trim() ?? FileSystem.CurrentDirectory;

            return new GenerateCommandContext(
                path,
                languageVersion,
                arguments.DISupport,
                arguments.Namespace?.Trim() ?? "StrawberryShake",
                arguments.PersistedQueryFile is { } fileName
                    ? FileSystem.CombinePath(path, fileName)
                    : null,
                arguments.Search,
                arguments.Force);
        }


        protected override async AsyncTask<bool> Compile(
            GenerateCommandContext context,
            string path,
            Config.Configuration config,
            ClientGenerator generator,
            IReadOnlyList<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            string hashFile = FileSystem.CombinePath(
               path,
               WellKnownDirectories.Generated,
               WellKnownFiles.Hash);

            if (await SkipCompileAsync(hashFile, documents, context.Force).ConfigureAwait(false))
            {
                return true;
            }

            generator.ModifyOptions(o =>
            {
                o.LanguageVersion = context.Language;
                o.EnableDISupport = context.DISupport;
            });

            generator.SetNamespace(context.Namespace);

            IReadOnlyList<HCError> validationErrors = generator.Validate();

            if (validationErrors.Count > 0)
            {
                foreach (HCError error in validationErrors)
                {
                    errors.Add(error);
                }
                return false;
            }

            await generator.BuildAsync().ConfigureAwait(false);
            await Task.Run(() => File.WriteAllText(hashFile, CreateHash(documents)))
                .ConfigureAwait(false);

            if (context.PersistedQueryFile is { } fileName)
            {
                using IActivity activity = Output.WriteActivity("Export queries", fileName);
                FileSystem.EnsureDirectoryExists(FileSystem.GetDirectoryName(fileName));
                await generator.ExportPersistedQueriesAsync(fileName).ConfigureAwait(false);
            }

            return true;
        }

        private async Task<bool> SkipCompileAsync(
            string hashFile,
            IReadOnlyList<DocumentInfo> documents,
            bool force)
        {
            if (force || !File.Exists(hashFile))
            {
                return false;
            }

            string newHash = CreateHash(documents);
            string currentHash = await Task.Run(() => File.ReadAllText(hashFile))
                .ConfigureAwait(false);

            return string.Equals(newHash, currentHash, StringComparison.Ordinal);
        }

        private string CreateHash(IReadOnlyList<DocumentInfo> documents)
        {
            string? version = GetType().Assembly?.GetName().Version?.ToString();
            string hash = string.Join("_", documents.Select(t => t.Hash));

            if (version is { })
            {
                hash = $"{version}__{hash}";
            }

            return hash;
        }
    }
}
