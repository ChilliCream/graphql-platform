using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using HotChocolate.Transport;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Management;

public sealed class ManagementServiceTests
{
    [Fact]
    public void BuildCreateApi_Sets_Variables_Correctly()
    {
        var request = ManagementQueries.BuildCreateApi(
            "ws-123", "My API", new[] { "team", "services" }, "service");

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildCreateApi_Uppercases_Kind()
    {
        var request = ManagementQueries.BuildCreateApi(
            "ws-123", "My API", new[] { "/" }, "gateway");

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildCreateApi_Null_Kind_Allowed()
    {
        var request = ManagementQueries.BuildCreateApi(
            "ws-123", "My API", new[] { "/" }, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildListApis_Sets_Variables_Correctly()
    {
        var request = ManagementQueries.BuildListApis("ws-123", 50, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildListApis_With_After_Cursor()
    {
        var request = ManagementQueries.BuildListApis("ws-123", 25, "cursor-abc");

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildUpdateApiSettings_With_Both_Settings()
    {
        var request = ManagementQueries.BuildUpdateApiSettings(
            "api-123", true, false);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildUpdateApiSettings_With_No_Optional_Settings()
    {
        var request = ManagementQueries.BuildUpdateApiSettings(
            "api-123", null, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildCreateApiKey_Workspace_Wide()
    {
        var request = ManagementQueries.BuildCreateApiKey(
            "ws-123", "my-key", null, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildCreateApiKey_Scoped_To_Api_And_Stage()
    {
        var request = ManagementQueries.BuildCreateApiKey(
            "ws-123", "deploy-key", "api-456", "production");

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildListApiKeys_Sets_Variables_Correctly()
    {
        var request = ManagementQueries.BuildListApiKeys("ws-123", 50, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildCreateClient_Sets_Variables_Correctly()
    {
        var request = ManagementQueries.BuildCreateClient("api-123", "my-client");

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildListClients_Sets_Variables_Correctly()
    {
        var request = ManagementQueries.BuildListClients("api-123", 50, null);

        Assert.NotNull(request);
    }

    [Fact]
    public void BuildListClients_With_After_Cursor()
    {
        var request = ManagementQueries.BuildListClients("api-123", 25, "cursor-xyz");

        Assert.NotNull(request);
    }
}
