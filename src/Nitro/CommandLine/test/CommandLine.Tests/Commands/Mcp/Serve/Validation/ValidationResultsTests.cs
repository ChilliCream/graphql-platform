using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Validation;

public sealed class ValidationResultsTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void SchemaChangeEntry_Record_Properties()
    {
        // act
        var entry = new SchemaChangeEntry("BREAKING", "FieldRemovedChange", "User.name",
            "Field 'name' was removed from type 'User'.");

        // assert
        Assert.Equal("BREAKING", entry.Severity);
        Assert.Equal("FieldRemovedChange", entry.ChangeType);
        Assert.Equal("User.name", entry.Coordinate);
        Assert.Equal("Field 'name' was removed from type 'User'.", entry.Description);
    }

    [Fact]
    public void SchemaChangeEntry_Null_Coordinate()
    {
        // act
        var entry = new SchemaChangeEntry("SAFE", "UnionMemberAdded", null,
            "Union member 'Dog' was added.");

        // assert
        Assert.Null(entry.Coordinate);
    }

    [Fact]
    public void ValidationError_Record_With_All_Properties()
    {
        // act
        var error = new ValidationError(
            "SyntaxError", "Unexpected token",
            Line: 10, Column: 5, Position: 42,
            Details: ["detail1", "detail2"]);

        // assert
        Assert.Equal("SyntaxError", error.Type);
        Assert.Equal("Unexpected token", error.Message);
        Assert.Equal(10, error.Line);
        Assert.Equal(5, error.Column);
        Assert.Equal(42, error.Position);
        Assert.Equal(2, error.Details!.Count);
    }

    [Fact]
    public void ValidationError_Record_With_Defaults()
    {
        // act
        var error = new ValidationError("InternalError", "Something went wrong.");

        // assert
        Assert.Null(error.Line);
        Assert.Null(error.Column);
        Assert.Null(error.Position);
        Assert.Null(error.Details);
    }

    [Fact]
    public void ClientValidationError_Record_With_All_Properties()
    {
        // act
        var locations = new List<ErrorLocation>
        {
            new(1, 10),
            new(3, 20)
        };
        var error = new ClientValidationError(
            "OperationValidationError", "Field not found",
            Hash: "abc123", Locations: locations);

        // assert
        Assert.Equal("OperationValidationError", error.Type);
        Assert.Equal("Field not found", error.Message);
        Assert.Equal("abc123", error.Hash);
        Assert.Equal(2, error.Locations!.Count);
        Assert.Equal(1, error.Locations[0].Line);
        Assert.Equal(10, error.Locations[0].Column);
    }

    [Fact]
    public void ClientValidationError_Record_With_Defaults()
    {
        // act
        var error = new ClientValidationError("Timeout", "Timed out.");

        // assert
        Assert.Null(error.Hash);
        Assert.Null(error.Locations);
    }

    [Fact]
    public void ErrorLocation_Record_Properties()
    {
        // act
        var loc = new ErrorLocation(42, 7);

        // assert
        Assert.Equal(42, loc.Line);
        Assert.Equal(7, loc.Column);
    }

    [Fact]
    public void SchemaValidationResult_Serialization_Roundtrip()
    {
        // arrange
        var original = new SchemaValidationResult(
            Valid: true,
            Changes:
            [
                new SchemaChangeEntry("BREAKING", "FieldRemovedChange", "User.name",
                    "Field removed")
            ],
            Errors: []);

        // act
        var json = JsonSerializer.Serialize(original, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SchemaValidationResult>(
            json, s_jsonOptions);

        // assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Valid);
        Assert.Single(deserialized.Changes);
        Assert.Equal("BREAKING", deserialized.Changes[0].Severity);
        Assert.Equal("FieldRemovedChange", deserialized.Changes[0].ChangeType);
        Assert.Equal("User.name", deserialized.Changes[0].Coordinate);
        Assert.Empty(deserialized.Errors);
    }

    [Fact]
    public void ClientValidationResult_Serialization_Roundtrip()
    {
        // arrange
        var original = new ClientValidationResult(
            Valid: false,
            Errors:
            [
                new ClientValidationError(
                    "OperationValidationError", "Field not found",
                    Hash: "abc123",
                    Locations: [new ErrorLocation(1, 5)])
            ]);

        // act
        var json = JsonSerializer.Serialize(original, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClientValidationResult>(
            json, s_jsonOptions);

        // assert
        Assert.NotNull(deserialized);
        Assert.False(deserialized.Valid);
        Assert.Single(deserialized.Errors);
        Assert.Equal("OperationValidationError", deserialized.Errors[0].Type);
        Assert.Equal("abc123", deserialized.Errors[0].Hash);
        var locations = deserialized.Errors[0].Locations;
        Assert.NotNull(locations);
        Assert.Single(locations);
        Assert.Equal(1, locations[0].Line);
        Assert.Equal(5, locations[0].Column);
    }

    [Fact]
    public void SchemaValidationResult_With_Errors_Roundtrip()
    {
        // arrange
        var original = new SchemaValidationResult(
            Valid: false,
            Changes: [],
            Errors:
            [
                new ValidationError("SyntaxError", "Unexpected token",
                    Line: 10, Column: 5, Position: 42,
                    Details: ["ERROR: missing closing brace"])
            ]);

        // act
        var json = JsonSerializer.Serialize(original, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SchemaValidationResult>(
            json, s_jsonOptions);

        // assert
        Assert.NotNull(deserialized);
        Assert.False(deserialized.Valid);
        Assert.Empty(deserialized.Changes);
        Assert.Single(deserialized.Errors);
        var err = deserialized.Errors[0];
        Assert.Equal("SyntaxError", err.Type);
        Assert.Equal(10, err.Line);
        Assert.Equal(5, err.Column);
        Assert.Equal(42, err.Position);
        Assert.Single(err.Details!);
    }

    [Fact]
    public void SchemaChangeEntry_Equality()
    {
        // arrange
        var a = new SchemaChangeEntry("BREAKING", "FieldRemovedChange", "User.name", "desc");
        var b = new SchemaChangeEntry("BREAKING", "FieldRemovedChange", "User.name", "desc");

        // assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void ClientValidationError_Equality()
    {
        // arrange
        var a = new ClientValidationError("Timeout", "Timed out.");
        var b = new ClientValidationError("Timeout", "Timed out.");

        // assert
        Assert.Equal(a, b);
    }
}
