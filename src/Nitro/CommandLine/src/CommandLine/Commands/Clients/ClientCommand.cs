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
    public ClientCommand(
        PublishClientCommand publishClientCommand,
        UnpublishClientCommand unpublishClientCommand,
        ValidateClientCommand validateClientCommand,
        UploadClientCommand uploadClientCommand,
        CreateClientCommand createClientCommand,
        DeleteClientCommand deleteClientCommand,
        ListClientCommand listClientCommand,
        ShowClientCommand showClientCommand,
        DownloadClientCommand downloadClientCommand)
        : base("client")
    {
        Description = "Manage clients.";

        Subcommands.Add(publishClientCommand);
        Subcommands.Add(unpublishClientCommand);
        Subcommands.Add(validateClientCommand);
        Subcommands.Add(uploadClientCommand);
        Subcommands.Add(createClientCommand);
        Subcommands.Add(deleteClientCommand);
        Subcommands.Add(listClientCommand);
        Subcommands.Add(showClientCommand);
        Subcommands.Add(downloadClientCommand);
    }
}
