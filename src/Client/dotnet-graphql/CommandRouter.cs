using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;

namespace StrawberryShake.Tools
{
    [Command(ThrowOnUnexpectedArgument = false)]
    public class CommandRouter
        : ICommand
    {
        [Argument(0, "command")]
        [Required]
        public Command Command { get; set; }

        public string[]? RemainingArgs { get; set; }

        public Task<int> OnExecute()
        {
            switch (Command)
            {
                case Command.Init:
                    return ExecuteAsync<InitCommand>(RemainingArgs);

                case Command.Update:
                    return ExecuteAsync<UpdateCommand>(RemainingArgs);

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
