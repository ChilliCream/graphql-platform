using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public class CompileCommand
        : CompileCommandBase
    {
        protected override Task<bool> Compile(
            string path,
            Configuration config,
            ClientGenerator generator)
        {
            IReadOnlyList<HCError> validationErrors = generator.Validate();
            if (validationErrors.Count > 0)
            {
                WriteErrors(validationErrors);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
