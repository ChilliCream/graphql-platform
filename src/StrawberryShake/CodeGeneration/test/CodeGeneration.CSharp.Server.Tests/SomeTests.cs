using System.Diagnostics;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StreamJsonRpc;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ServerTests
{
    [Fact]
    public async Task SetConfiguration_FileIsNull()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = await server.SetConfigurationAsync(null!);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task SetConfiguration_FileIsEmpty()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = await server.SetConfigurationAsync(string.Empty);

        // assert
        response.MatchSnapshot();
    }

    public async Task SetDocuments_FilesIsNull()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = await server.SetDocumentsAsync(null);

        // assert
        response.MatchSnapshot();
    }
}
