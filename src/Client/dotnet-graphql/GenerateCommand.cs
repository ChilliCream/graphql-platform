using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;
using IOPath = System.IO.Path;

namespace StrawberryShake.Tools
{
    public class GenerateCommand
        : CompileCommandBase
    {
        [Option("-l|--LanguageVersion")]
        public string? LanguageVersion { get; set; }

        [Option("-d|--DISupport")]
        public bool DISupport { get; set; }

        [Option("-n|--Namespace")]
        public string? Namespace { get; set; }

        [Option("-f|--Force")]
        public bool Force { get; set; }

        protected override async Task<bool> Compile(
            string path,
            IReadOnlyList<DocumentInfo> documents,
            Configuration config,
            ClientGenerator generator)
        {
            string hashFile = IOPath.Combine(
                path,
                WellKnownDirectories.Generated,
                WellKnownFiles.Hash);

            if (await SkipCompileAsync(path, hashFile, documents))
            {
                return true;
            }

            if (Enum.TryParse(LanguageVersion, true, out LanguageVersion version))
            {
                generator.ModifyOptions(o => o.LanguageVersion = version);
            }

            generator.ModifyOptions(o => o.EnableDISupport = DISupport);

            if (Namespace is { })
            {
                generator.SetNamespace(Namespace);
            }

            IReadOnlyList<HCError> validationErrors = generator.Validate();

            if (validationErrors.Count > 0)
            {
                WriteErrors(validationErrors);
                return false;
            }

            await generator.BuildAsync();
            await File.WriteAllTextAsync(
                hashFile,
                string.Join(string.Empty, documents.Select(t => t.Hash)));
            return true;
        }

        private async Task<bool> SkipCompileAsync(
            string path,
            string hashFile,
            IReadOnlyList<DocumentInfo> documents)
        {
            if (Force || !File.Exists(hashFile))
            {
                return false;
            }

            string newHash = string.Join(string.Empty, documents.Select(t => t.Hash));
            string currentHash = await File.ReadAllTextAsync(hashFile);

            return string.Equals(newHash, currentHash, StringComparison.Ordinal);
        }

        protected override void WriteCompileStartedMessage()
        {
            Console.WriteLine("Generate client started.");
        }

        protected override void WriteCompileCompletedMessage(
            string path, Stopwatch stopwatch)
        {
            Console.WriteLine(
                $"Generate client completed in {stopwatch.ElapsedMilliseconds} ms " +
                $"for {path}.");
        }
    }
}
