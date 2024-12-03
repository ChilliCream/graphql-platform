using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class CostTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Request_No_Cost_Header()
    {
        // arrange
        var server = CreateStarWarsServer();

        var uri = new Uri("http://localhost:5000/graphql");

        var requestBody =
            """
            {
                "query" : "query Test($id: String!){human(id: $id){name}}"
                "variables" : { "id" : "1000" }
            }
            """;

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // act
        using var httpClient = server.CreateClient();
        var response = await httpClient.PostAsync(uri, content);

        // assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(response);
        result?.RootElement.MatchSnapshot();
    }

    [Fact]
    public async Task Request_Report_Cost_Header()
    {
        // arrange
        var server = CreateStarWarsServer();

        var uri = new Uri("http://localhost:5000/graphql");

        var requestBody =
            """
            {
                "query" : "query Test($id: String!){human(id: $id){name}}"
                "variables" : { "id" : "1000" }
            }
            """;

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        content.Headers.Add(HttpHeaderKeys.Cost, HttpHeaderValues.ReportCost);

        // act
        using var httpClient = server.CreateClient();
        var response = await httpClient.PostAsync(uri, content);

        // assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(response);
        result?.RootElement.MatchSnapshot();
    }

    [Fact]
    public async Task Request_Validate_Cost_Header()
    {
        // arrange
        var server = CreateStarWarsServer();

        var uri = new Uri("http://localhost:5000/graphql");

        var requestBody =
            """
            {
                "query" : "query Test($id: String!){human(id: $id){name}}"
                "variables" : { "id" : "1000" }
            }
            """;

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        content.Headers.Add(HttpHeaderKeys.Cost, HttpHeaderValues.ValidateCost);

        // act
        using var httpClient = server.CreateClient();
        var response = await httpClient.PostAsync(uri, content);

        // assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(response);
        result?.RootElement.MatchSnapshot();
    }
}
