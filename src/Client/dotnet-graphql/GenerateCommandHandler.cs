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

            return new GenerateCommandContext(
                arguments.Path.Value()?.Trim() ?? FileSystem.CurrentDirectory,
                languageVersion,
                arguments.DISupport.HasValue(),
                arguments.Namespace.Value()?.Trim() ?? "StrawberryShake",
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

            if (await SkipCompileAsync(path, hashFile, documents, context.Force))
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

            await generator.BuildAsync();
            await File.WriteAllTextAsync(hashFile, CreateHash(documents));
            return true;
        }

        private async Task<bool> SkipCompileAsync(
            string path,
            string hashFile,
            IReadOnlyList<DocumentInfo> documents,
            bool force)
        {
            if (force || !File.Exists(hashFile))
            {
                return false;
            }

            string newHash = CreateHash(documents);
            string currentHash = await File.ReadAllTextAsync(hashFile);

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
