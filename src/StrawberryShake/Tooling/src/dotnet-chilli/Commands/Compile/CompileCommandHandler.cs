using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using StrawberryShake.Tools.Abstractions;
using HCError = HotChocolate.IError;
using IHttpClientFactory = StrawberryShake.Tools.Abstractions.IHttpClientFactory;

namespace StrawberryShake.Tools.Commands.Compile
{
    public class CompileCommandHandler : CompileCommandHandlerBase<Options.Compile, CompileCommandContext>
    {
        public CompileCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output)
            : base(fileSystem, httpClientFactory, configurationStore, output)
        {
        }

        protected override CompileCommandContext CreateContext(Options.Compile arguments)
        {
            return new(
                arguments.Path?.Trim() ?? FileSystem.CurrentDirectory,
                string.IsNullOrWhiteSpace(arguments.Search));
        }

        protected override async ValueTask<bool> Compile(
            CompileCommandContext context,
            string path,
            Config.Configuration config,
            ClientGenerator generator,
            IReadOnlyList<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            var validationErrors = generator.Validate();

            if (validationErrors.Count > 0)
            {
                foreach (HCError error in validationErrors)
                {
                    errors.Add(error);
                }

                return false;
            }

            return true;
        }
    }
}
