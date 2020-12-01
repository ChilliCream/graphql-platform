using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public class CompileCommandHandler
        : CompileCommandHandlerBase<CompileCommandArguments, CompileCommandContext>
    {
        public CompileCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output)
            : base(fileSystem, httpClientFactory, configurationStore, output)
        {
        }

        protected override CompileCommandContext CreateContext(
            CompileCommandArguments arguments)
        {
            return new CompileCommandContext(
                arguments.Path.Value()?.Trim() ?? FileSystem.CurrentDirectory,
                arguments.Search.HasValue());
        }

        protected override Task<bool> Compile(
            CompileCommandContext context,
            string path,
            Configuration config,
            ClientGenerator generator,
            IReadOnlyList<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            IReadOnlyList<HCError> validationErrors = generator.Validate();

            if (validationErrors.Count > 0)
            {
                foreach (HCError error in validationErrors)
                {
                    errors.Add(error);
                }
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
