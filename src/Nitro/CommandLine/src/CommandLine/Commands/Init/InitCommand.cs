using ChilliCream.Nitro.CommandLine.Commands.Init.Mcp;

namespace ChilliCream.Nitro.CommandLine.Commands.Init;

internal sealed class InitCommand : Command
{
    public InitCommand() : base("init")
    {
        Description = "Initialize project configuration files";

        AddCommand(new InitMcpCommand());
    }
}
