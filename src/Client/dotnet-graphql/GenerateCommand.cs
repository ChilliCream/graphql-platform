using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public class GenerateCommand
        : CompileCommandBase
    {
        protected override async Task<bool> Compile(
            string path,
            Configuration config,
            ClientGenerator generator)
        {
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
