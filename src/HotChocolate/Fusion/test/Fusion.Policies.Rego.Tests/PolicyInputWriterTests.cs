using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class PolicyInputWriterTests
{
    [Fact]
    public void WriteDefaultSubject_Should_WriteIdRolesAndClaims()
    {
        // arrange
        var identity = new ClaimsIdentity(
            [
                new Claim("name", "u1"),
                new Claim("role", "admin"),
                new Claim("role", "editor"),
                new Claim("tenant", "acme")
            ],
            authenticationType: "test",
            nameType: "name",
            roleType: "role");

        // act
        var json = WriteJson(
            writer => PolicyInputWriter.WriteDefaultSubject(
                writer,
                new ClaimsPrincipal(identity)));

        // assert
        json.MatchInlineSnapshot(
            """
            {"id":"u1","roles":["admin","editor"],"claims":{"name":"u1","tenant":"acme"}}
            """);
    }

    private static string WriteJson(Action<JsonWriter> write)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { Indented = false });
        write(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
