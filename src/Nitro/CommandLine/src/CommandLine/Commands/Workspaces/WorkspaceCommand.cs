#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class WorkspaceCommand : Command
{
    public WorkspaceCommand(
        CreateWorkspaceCommand createWorkspaceCommand,
        CurrentWorkspaceCommand currentWorkspaceCommand,
        SetDefaultWorkspaceCommand setDefaultWorkspaceCommand,
        ListWorkspaceCommand listWorkspaceCommand,
        ShowWorkspaceCommand showWorkspaceCommand)
        : base("workspace")
    {
        Description = "Manage workspaces";

        Subcommands.Add(createWorkspaceCommand);
        Subcommands.Add(currentWorkspaceCommand);
        Subcommands.Add(setDefaultWorkspaceCommand);
        Subcommands.Add(listWorkspaceCommand);
        Subcommands.Add(showWorkspaceCommand);
    }
}
