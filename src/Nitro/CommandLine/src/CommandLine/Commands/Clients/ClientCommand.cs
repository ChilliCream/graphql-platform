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
        Description = "Manage clients";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new PublishClientCommand());
        AddCommand(new UnpublishClientCommand());
        AddCommand(new ValidateClientCommand());
        AddCommand(new UploadClientCommand());
        AddCommand(new CreateClientCommand());
        AddCommand(new DeleteClientCommand());
        AddCommand(new ListClientCommand());
        AddCommand(new ShowClientCommand());
        AddCommand(new DownloadClientCommand());
    }
}
