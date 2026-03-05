using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Validation;

public sealed class SchemaChangeMapperTests
{
    [Fact]
    public void Map_TypeSystemMemberRemovedChange_Returns_Breaking()
    {
        // arrange
        var change = new StubTypeSystemMemberRemovedChange(
            SchemaChangeSeverity.Breaking, "User");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("BREAKING", result.Severity);
        Assert.Equal("TypeSystemMemberRemovedChange", result.ChangeType);
        Assert.Equal("User", result.Coordinate);
        Assert.Equal("Schema member 'User' was removed.", result.Description);
    }

    [Fact]
    public void Map_TypeSystemMemberAddedChange_Returns_Safe()
    {
        // arrange
        var change = new StubTypeSystemMemberAddedChange(
            SchemaChangeSeverity.Safe, "Product");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("TypeSystemMemberAddedChange", result.ChangeType);
        Assert.Equal("Product", result.Coordinate);
        Assert.Equal("Schema member 'Product' was added.", result.Description);
    }

    [Fact]
    public void Map_FieldRemovedChange_Returns_Breaking()
    {
        // arrange
        var change = new StubFieldRemovedChange(
            SchemaChangeSeverity.Breaking, "User.name", "User", "name");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("BREAKING", result.Severity);
        Assert.Equal("FieldRemovedChange", result.ChangeType);
        Assert.Equal("User.name", result.Coordinate);
        Assert.Equal("Field 'name' was removed from type 'User'.", result.Description);
    }

    [Fact]
    public void Map_FieldAddedChange_Returns_Safe()
    {
        // arrange
        var change = new StubFieldAddedChange(
            SchemaChangeSeverity.Safe, "User.email", "User", "email");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("FieldAddedChange", result.ChangeType);
        Assert.Equal("User.email", result.Coordinate);
        Assert.Equal("Field 'email' was added to type 'User'.", result.Description);
    }

    [Fact]
    public void Map_OutputFieldChanged_Returns_Dangerous()
    {
        // arrange
        var change = new StubOutputFieldChanged(
            SchemaChangeSeverity.Dangerous, "User.age", "age");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("DANGEROUS", result.Severity);
        Assert.Equal("OutputFieldChanged", result.ChangeType);
        Assert.Equal("User.age", result.Coordinate);
        Assert.Equal("Field 'age' at 'User.age' was modified.", result.Description);
    }

    [Fact]
    public void Map_InputFieldChanged_Returns_Dangerous()
    {
        // arrange
        var change = new StubInputFieldChanged(
            SchemaChangeSeverity.Dangerous, "CreateUserInput.name", "name");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("DANGEROUS", result.Severity);
        Assert.Equal("InputFieldChanged", result.ChangeType);
        Assert.Equal("CreateUserInput.name", result.Coordinate);
        Assert.Equal("Input field 'name' at 'CreateUserInput.name' was modified.",
            result.Description);
    }

    [Fact]
    public void Map_EnumValueRemoved_Returns_Breaking()
    {
        // arrange
        var change = new StubEnumValueRemoved(
            SchemaChangeSeverity.Breaking, "Status.ACTIVE");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("BREAKING", result.Severity);
        Assert.Equal("EnumValueRemoved", result.ChangeType);
        Assert.Equal("Status.ACTIVE", result.Coordinate);
        Assert.Equal("Enum value 'Status.ACTIVE' was removed.", result.Description);
    }

    [Fact]
    public void Map_EnumValueAdded_Returns_Safe()
    {
        // arrange
        var change = new StubEnumValueAdded(
            SchemaChangeSeverity.Safe, "Status.PENDING");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("EnumValueAdded", result.ChangeType);
        Assert.Equal("Status.PENDING", result.Coordinate);
        Assert.Equal("Enum value 'Status.PENDING' was added.", result.Description);
    }

    [Fact]
    public void Map_UnionMemberRemoved_Returns_Breaking()
    {
        // arrange
        var change = new StubUnionMemberRemoved(
            SchemaChangeSeverity.Breaking, "Cat");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("BREAKING", result.Severity);
        Assert.Equal("UnionMemberRemoved", result.ChangeType);
        Assert.Null(result.Coordinate);
        Assert.Equal("Union member 'Cat' was removed.", result.Description);
    }

    [Fact]
    public void Map_UnionMemberAdded_Returns_Safe()
    {
        // arrange
        var change = new StubUnionMemberAdded(
            SchemaChangeSeverity.Safe, "Dog");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("UnionMemberAdded", result.ChangeType);
        Assert.Null(result.Coordinate);
        Assert.Equal("Union member 'Dog' was added.", result.Description);
    }

    [Fact]
    public void Map_ObjectModifiedChange_Returns_Dangerous()
    {
        // arrange
        var change = new StubObjectModifiedChange(
            SchemaChangeSeverity.Dangerous, "User");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("DANGEROUS", result.Severity);
        Assert.Equal("ObjectModifiedChange", result.ChangeType);
        Assert.Equal("User", result.Coordinate);
        Assert.Equal("Object type 'User' was modified.", result.Description);
    }

    [Fact]
    public void Map_DirectiveModifiedChange_Returns_Dangerous()
    {
        // arrange
        var change = new StubDirectiveModifiedChange(
            SchemaChangeSeverity.Dangerous, "@deprecated");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("DANGEROUS", result.Severity);
        Assert.Equal("DirectiveModifiedChange", result.ChangeType);
        Assert.Equal("@deprecated", result.Coordinate);
        Assert.Equal("Directive '@deprecated' was modified.", result.Description);
    }

    [Fact]
    public void Map_EnumModifiedChange_Returns_Dangerous()
    {
        // arrange
        var change = new StubEnumModifiedChange(
            SchemaChangeSeverity.Dangerous, "Status");

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("DANGEROUS", result.Severity);
        Assert.Equal("EnumModifiedChange", result.ChangeType);
        Assert.Equal("Status", result.Coordinate);
        Assert.Equal("Enum 'Status' was modified.", result.Description);
    }

    [Fact]
    public void Map_UnknownSchemaChange_Uses_TypeName()
    {
        // arrange
        var change = new StubUnknownSchemaChange(SchemaChangeSeverity.Safe);

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("StubUnknownSchemaChange", result.ChangeType);
        Assert.Null(result.Coordinate);
        Assert.Contains("StubUnknownSchemaChange", result.Description);
    }

    [Fact]
    public void Map_UnknownLogEntry_Defaults_To_Safe()
    {
        // arrange
        var change = new StubUnknownLogEntry();

        // act
        var result = SchemaChangeMapper.Map(change);

        // assert
        Assert.Equal("SAFE", result.Severity);
        Assert.Equal("StubUnknownLogEntry", result.ChangeType);
        Assert.Null(result.Coordinate);
    }

    [Fact]
    public void MapAll_Orders_Breaking_Before_Dangerous_Before_Safe()
    {
        // arrange
        var entries = new ISchemaChangeLogEntry[]
        {
            new StubFieldAddedChange(SchemaChangeSeverity.Safe, "User.email", "User", "email"),
            new StubFieldRemovedChange(SchemaChangeSeverity.Breaking, "User.name", "User", "name"),
            new StubOutputFieldChanged(SchemaChangeSeverity.Dangerous, "User.age", "age")
        };

        // act
        var result = SchemaChangeMapper.MapAll(entries);

        // assert
        Assert.Equal(3, result.Count);
        Assert.Equal("BREAKING", result[0].Severity);
        Assert.Equal("DANGEROUS", result[1].Severity);
        Assert.Equal("SAFE", result[2].Severity);
    }

    [Fact]
    public void MapAll_Empty_Returns_Empty_List()
    {
        // act
        var result = SchemaChangeMapper.MapAll([]);

        // assert
        Assert.Empty(result);
    }

    [Fact]
    public void MapAll_Multiple_Same_Severity_Preserves_All()
    {
        // arrange
        var entries = new ISchemaChangeLogEntry[]
        {
            new StubFieldRemovedChange(SchemaChangeSeverity.Breaking, "A.x", "A", "x"),
            new StubFieldRemovedChange(SchemaChangeSeverity.Breaking, "B.y", "B", "y")
        };

        // act
        var result = SchemaChangeMapper.MapAll(entries);

        // assert
        Assert.Equal(2, result.Count);
        Assert.All(result, entry => Assert.Equal("BREAKING", entry.Severity));
    }

    // --- Stubs ---

    private sealed class StubTypeSystemMemberAddedChange(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, ITypeSystemMemberAddedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "TypeSystemMemberAddedChange";
    }

    private sealed class StubTypeSystemMemberRemovedChange(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, ITypeSystemMemberRemovedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "TypeSystemMemberRemovedChange";
    }

    private sealed class StubFieldRemovedChange(
        SchemaChangeSeverity severity, string coordinate,
        string typeName, string fieldName)
        : ISchemaChangeLogEntry, IFieldRemovedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string TypeName => typeName;
        public string FieldName => fieldName;
        public string __typename => "FieldRemovedChange";
    }

    private sealed class StubFieldAddedChange(
        SchemaChangeSeverity severity, string coordinate,
        string typeName, string fieldName)
        : ISchemaChangeLogEntry, IFieldAddedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string TypeName => typeName;
        public string FieldName => fieldName;
        public string __typename => "FieldAddedChange";
    }

    private sealed class StubOutputFieldChanged(
        SchemaChangeSeverity severity, string coordinate,
        string fieldName) : ISchemaChangeLogEntry, IOutputFieldChanged
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string FieldName => fieldName;
        public string __typename => "OutputFieldChanged";
        public IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_Changes_3>
            Changes => [];
    }

    private sealed class StubInputFieldChanged(
        SchemaChangeSeverity severity, string coordinate,
        string fieldName) : ISchemaChangeLogEntry, IInputFieldChanged
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string FieldName => fieldName;
        public string __typename => "InputFieldChanged";
        public IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_Changes_2>
            Changes => [];
    }

    private sealed class StubEnumValueRemoved(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, IEnumValueRemoved
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "EnumValueRemoved";
    }

    private sealed class StubEnumValueAdded(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, IEnumValueAdded
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "EnumValueAdded";
    }

    private sealed class StubUnionMemberRemoved(
        SchemaChangeSeverity severity, string typeName)
        : ISchemaChangeLogEntry, IUnionMemberRemoved
    {
        public SchemaChangeSeverity Severity => severity;
        public string TypeName => typeName;
        public string __typename => "UnionMemberRemoved";
    }

    private sealed class StubUnionMemberAdded(
        SchemaChangeSeverity severity, string typeName)
        : ISchemaChangeLogEntry, IUnionMemberAdded
    {
        public SchemaChangeSeverity Severity => severity;
        public string TypeName => typeName;
        public string __typename => "UnionMemberAdded";
    }

    private sealed class StubObjectModifiedChange(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, IObjectModifiedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "ObjectModifiedChange";
        public IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_4>
            Changes => [];
    }

    private sealed class StubDirectiveModifiedChange(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, IDirectiveModifiedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "DirectiveModifiedChange";
        public IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes>
            Changes => [];
    }

    private sealed class StubEnumModifiedChange(
        SchemaChangeSeverity severity, string coordinate)
        : ISchemaChangeLogEntry, IEnumModifiedChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string Coordinate => coordinate;
        public string __typename => "EnumModifiedChange";
        public IReadOnlyList<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_1>
            Changes => [];
    }

    private sealed class StubUnknownSchemaChange(SchemaChangeSeverity severity)
        : ISchemaChangeLogEntry, ISchemaChange
    {
        public SchemaChangeSeverity Severity => severity;
        public string __typename => "StubUnknownSchemaChange";
    }

    private sealed class StubUnknownLogEntry : ISchemaChangeLogEntry
    {
        public string __typename => "StubUnknownLogEntry";
    }
}
