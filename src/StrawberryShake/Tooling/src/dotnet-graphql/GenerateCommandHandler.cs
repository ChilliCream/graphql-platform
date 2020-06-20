using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public class GenerateCommandHandler
        : CompileCommandHandlerBase<GenerateCommandArguments, GenerateCommandContext>
    {
        public GenerateCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output)
            : base(fileSystem, httpClientFactory, configurationStore, output)
        {
        }

        protected override string ActivityName => "Generate client";

        protected override GenerateCommandContext CreateContext(
            GenerateCommandArguments arguments)
        {
            LanguageVersion languageVersion =
                (!arguments.Language.HasValue() || arguments.Language.Value()?.Trim() == "7.3")
                ? LanguageVersion.CSharp_7_3
                : LanguageVersion.CSharp_8_0;

            string path = arguments.Path.Value()?.Trim() ?? FileSystem.CurrentDirectory;

            return new GenerateCommandContext(
                path,
                languageVersion,
                arguments.DISupport.HasValue(),
                arguments.Namespace.Value()?.Trim() ?? "StrawberryShake",
                arguments.PersistedQueryFile.Value() is { } fileName
                    ? FileSystem.CombinePath(path, fileName)
                    : null,
                arguments.Search.HasValue(),
                arguments.Force.HasValue());
        }

        protected override async Task<bool> Compile(
            GenerateCommandContext context,
            string path,
            Configuration config,
            ClientGenerator generator,
            IReadOnlyList<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            string hashFile = FileSystem.CombinePath(
               path,
               WellKnownDirectories.Generated,
               WellKnownFiles.Hash);

            if (await SkipCompileAsync(hashFile, documents, context.Force)
                .ConfigureAwait(false))
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
