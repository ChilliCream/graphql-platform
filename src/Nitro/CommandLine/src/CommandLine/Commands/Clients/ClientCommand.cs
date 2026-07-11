#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class ClientCommand : Command
{
    public ClientCommand() : base("client")
    {
        Description = "Manage clients.";

        Subcommands.Add(new PublishClientCommand());
        Subcommands.Add(new UnpublishClientCommand());
        Subcommands.Add(new ValidateClientCommand());
        Subcommands.Add(new UploadClientCommand());
        Subcommands.Add(new CreateClientCommand());
        Subcommands.Add(new DeleteClientCommand());
        Subcommands.Add(new ListClientCommand());
        Subcommands.Add(new ShowClientCommand());
        Subcommands.Add(new DownloadClientCommand());
    }
}
