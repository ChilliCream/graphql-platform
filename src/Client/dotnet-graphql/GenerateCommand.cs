using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;

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

        protected override async Task<bool> Compile(
            string path,
            Configuration config,
            ClientGenerator generator)
        {
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
            return true;
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
