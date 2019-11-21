using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;

namespace StrawberryShake.Tools
{
    public class CommandRouter
        : ICommand
    {
        [Argument(0, "command")]
        [Required]
        public Command Command { get; set; }

        public string[]? RemainingArgs { get; set; }

        public Task<int> OnExecute()
        {
            RemainingArgs = RemainingArgs ?? Array.Empty<string>();

            switch (Command)
            {
                case Command.Init:
                case Command.Update:
                case Command.Compile:
                    return ExecuteAsync<CompileCommand>(RemainingArgs);

                case Command.Generate:
                    return ExecuteAsync<GenerateCommand>(RemainingArgs);

                default:
                    return Task.FromResult(1);
            }
        }
    }
}
