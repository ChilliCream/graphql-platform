namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// A message meant for the user rather than a stack trace: every command's
/// exception handling prints it and exits nonzero. Not sealed, so a caller
/// that has to tell one cause apart from the rest can derive from it (see
/// <see cref="Services.Workspace.AgentWorkspaceSchemaMismatchException"/>)
/// without losing that handling.
/// </summary>
public class ExitException : Exception
{
    public ExitException() : base("")
    {
    }

    public ExitException(string message) : base(message)
    {
    }
}
