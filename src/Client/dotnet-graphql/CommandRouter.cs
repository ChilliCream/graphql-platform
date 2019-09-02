using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;

namespace StrawberryShake.Tools
{
    [Command(ThrowOnUnexpectedArgument = false)]
    public class CommandRouter
        : ICommand
    {
        [Argument(0)]
        public Command Command { get; set; }

        public string[] RemainingArgs { get; set; }

        public Task<int> OnExecute()
        {
            switch (Command)
            {
                case Command.Compile:
                    return ExecuteAsync<CompileCommand>(RemainingArgs);

                default:
                    return Task.FromResult(1);
            }
        }
    }
}
