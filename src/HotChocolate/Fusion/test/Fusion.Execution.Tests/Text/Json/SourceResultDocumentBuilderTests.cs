using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultDocumentBuilderTests : FusionTestBase
{
    [Fact]
    public void Build_SimpleObject_ProducesValidJson()
    {
        // Arrange
        var operation = CreateOperation(
            """
            {
              userById(id: "1") {
                name
              }
            }
            """);

        using var builder = new SourceResultDocumentBuilder(operation, includeFlags: 0);

        // Root is already set up with the operation structure, now fill in values
        var userSelection = operation.RootSelectionSet.Selections[0];
        var userProperty = builder.Root.CreateProperty(userSelection, 0);

        // Create the user object
        var userObject = userProperty.CreateObjectValue(userSelection, 0);

        // Set the name property
        var userSelectionSet = operation.GetSelectionSet(userSelection);
        var nameProperty = userObject.CreateProperty(userSelectionSet.Selections[0], 0);
        nameProperty.SetStringValue("Alice");

        // Act
        using var document = builder.Build();

        // Assert
        Assert.NotNull(document);
        Assert.Equal(JsonValueKind.Object, document.Root.ValueKind);

        var user = document.Root.GetProperty("userById");
        Assert.Equal(JsonValueKind.Object, user.ValueKind);

        var name = user.GetProperty("name");
        Assert.Equal("Alice", name.AssertString());
    }

    [Fact]
    public void Build_NestedObject_ProducesValidJson()
    {
        // Arrange
        var operation = CreateOperation(
            """
            {
              userById(id: "1") {
                name
                username
              }
            }
            """);

        using var builder = new SourceResultDocumentBuilder(operation, includeFlags: 0);
        var userProperty = builder.Root.CreateProperty(operation.RootSelectionSet.Selections[0], 0);

        var userSelection = operation.RootSelectionSet.Selections[0];
        var userSelectionSet = operation.GetSelectionSet(userSelection);
        var userObject = userProperty.CreateObjectValue(userSelection, 0);

        var nameProperty = userObject.CreateProperty(userSelectionSet.Selections[0], 0);
        nameProperty.SetStringValue("John");

        var usernameProperty = userObject.CreateProperty(userSelectionSet.Selections[1], 1);
        usernameProperty.SetStringValue("john_doe");

        // Act
        using var document = builder.Build();

        // Assert
        Assert.NotNull(document);
        var userElement = document.Root.GetProperty("userById");
        Assert.Equal(JsonValueKind.Object, userElement.ValueKind);

        var name = userElement.GetProperty("name");
        Assert.Equal("John", name.AssertString());

        var username = userElement.GetProperty("username");
        Assert.Equal("john_doe", username.AssertString());
    }

    [Fact]
    public void Build_NullValue_ProducesValidJson()
    {
        // Arrange
        var operation = CreateOperation(
            """
            {
              userById(id: "1") {
                name
              }
            }
            """);

        using var builder = new SourceResultDocumentBuilder(operation, includeFlags: 0);
        var userProperty = builder.Root.CreateProperty(operation.RootSelectionSet.Selections[0], 0);
        userProperty.SetNullValue();

        // Act
        using var document = builder.Build();

        // Assert
        var user = document.Root.GetProperty("userById");
        Assert.Equal(JsonValueKind.Null, user.ValueKind);
    }

    [Fact]
    public void Build_MultipleProperties_ProducesValidJson()
    {
        // Arrange
        var operation = CreateOperation(
            """
            {
              userById(id: "1") {
                id
                name
                username
                birthdate
              }
            }
            """);

        using var builder = new SourceResultDocumentBuilder(operation, includeFlags: 0);
        var userProperty = builder.Root.CreateProperty(operation.RootSelectionSet.Selections[0], 0);

        var userSelection = operation.RootSelectionSet.Selections[0];
        var userSelectionSet = operation.GetSelectionSet(userSelection);
        var userObject = userProperty.CreateObjectValue(userSelection, 0);

        var idProperty = userObject.CreateProperty(userSelectionSet.Selections[0], 0);
        idProperty.SetStringValue("user-1");

        var nameProperty = userObject.CreateProperty(userSelectionSet.Selections[1], 1);
        nameProperty.SetStringValue("Alice");

        var usernameProperty = userObject.CreateProperty(userSelectionSet.Selections[2], 2);
        usernameProperty.SetStringValue("alice123");

        var birthdateProperty = userObject.CreateProperty(userSelectionSet.Selections[3], 3);
        birthdateProperty.SetStringValue("1990-01-01");

        // Act
        using var document = builder.Build();

        // Assert
        var user = document.Root.GetProperty("userById");
        Assert.Equal("user-1", user.GetProperty("id").AssertString());
        Assert.Equal("Alice", user.GetProperty("name").AssertString());
        Assert.Equal("alice123", user.GetProperty("username").AssertString());
        Assert.Equal("1990-01-01", user.GetProperty("birthdate").AssertString());
    }

    [Fact]
    public void Build_WithUsersConnection_ProducesValidJson()
    {
        // Arrange
        var operation = CreateOperation(
            """
            {
              users(first: 2) {
                nodes {
                  name
                }
              }
            }
            """);

        using var builder = new SourceResultDocumentBuilder(operation, includeFlags: 0);
        var usersProperty = builder.Root.CreateProperty(operation.RootSelectionSet.Selections[0], 0);

        var usersSelection = operation.RootSelectionSet.Selections[0];
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        var usersObject = usersProperty.CreateObjectValue(usersSelection, 0);

        var nodesProperty = usersObject.CreateProperty(usersSelectionSet.Selections[0], 0);
        var nodesArray = nodesProperty.CreateListValue(2);
        var arrayElements = nodesArray.EnumerateArray().ToArray();

        // First user
        var nodesSelection = usersSelectionSet.Selections[0];
        var nodeSelectionSet = operation.GetSelectionSet(nodesSelection);
        var user1 = arrayElements[0].CreateObjectValue(nodesSelection, 0);
        var name1 = user1.CreateProperty(nodeSelectionSet.Selections[0], 0);
        name1.SetStringValue("Alice");

        // Second user
        var user2 = arrayElements[1].CreateObjectValue(nodesSelection, 0);
        var name2 = user2.CreateProperty(nodeSelectionSet.Selections[0], 0);
        name2.SetStringValue("Bob");

        // Act
        using var document = builder.Build();

        // Assert
        var users = document.Root.GetProperty("users");
        var nodes = users.GetProperty("nodes");
        Assert.Equal(JsonValueKind.Array, nodes.ValueKind);
        Assert.Equal(2, nodes.GetArrayLength());

        var nodesArray2 = nodes.EnumerateArray().ToArray();
        Assert.Equal("Alice", nodesArray2[0].GetProperty("name").AssertString());
        Assert.Equal("Bob", nodesArray2[1].GetProperty("name").AssertString());
    }

    [Fact]
    public void MetaDb_StoresAndRetrievesData_Correctly()
    {
        // Arrange - Create a MetaDb directly without going through SourceResultDocumentBuilder
        var metaDb = new SourceResultDocumentBuilder.MetaDb();

        // Act - Append some rows to MetaDb
        var index0 = metaDb.Append(ElementTokenType.StartObject, sizeOrLength: 1, rows: 4);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 42);
        var index2 = metaDb.Append(ElementTokenType.String, location: 100, sizeOrLength: 10);
        var index3 = metaDb.Append(ElementTokenType.EndObject, sizeOrLength: 1, rows: 4);

        // Assert
        Assert.Equal(0, index0);
        Assert.Equal(1, index1);
        Assert.Equal(2, index2);
        Assert.Equal(3, index3);

        var row0 = metaDb.Get(0);
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);
        Assert.Equal(1, row0.SizeOrLength);
        Assert.Equal(4, row0.NumberOfRows);

        var row1 = metaDb.Get(1);
        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);
        Assert.Equal(42, row1.Location);

        var row2 = metaDb.Get(2);
        Assert.Equal(ElementTokenType.String, row2.TokenType);
        Assert.Equal(100, row2.Location);
        Assert.Equal(10, row2.SizeOrLength);
    }

    [Fact]
    public void MetaDb_SetMethods_UpdateRowsCorrectly()
    {
        // Arrange - Create a MetaDb directly
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index = metaDb.Append(ElementTokenType.StartObject, location: 0, sizeOrLength: 1, rows: 1);

        // Act
        metaDb.SetLocation(index, 999);
        metaDb.SetSizeOrLength(index, 5);
        metaDb.SetRows(index, 10);
        metaDb.SetElementTokenType(index, ElementTokenType.StartArray);

        // Assert
        var row = metaDb.Get(index);
        Assert.Equal(999, row.Location);
        Assert.Equal(5, row.SizeOrLength);
        Assert.Equal(10, row.NumberOfRows);
        Assert.Equal(ElementTokenType.StartArray, row.TokenType);
    }

    [Fact]
    public void MetaDb_MultipleRows_DoNotInterfereWithEachOther()
    {
        // Arrange - Create multiple rows with different values
        var metaDb = new SourceResultDocumentBuilder.MetaDb();

        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 10, sizeOrLength: 5, rows: 3);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 20, sizeOrLength: 8, rows: 1);
        var index2 = metaDb.Append(ElementTokenType.String, location: 30, sizeOrLength: 12, rows: 1);
        var index3 = metaDb.Append(ElementTokenType.StartArray, location: 40, sizeOrLength: 15, rows: 6);
        var index4 = metaDb.Append(ElementTokenType.Number, location: 50, sizeOrLength: 4, rows: 1);

        // Act - Read all rows
        var row0 = metaDb.Get(index0);
        var row1 = metaDb.Get(index1);
        var row2 = metaDb.Get(index2);
        var row3 = metaDb.Get(index3);
        var row4 = metaDb.Get(index4);

        // Assert - Verify each row has correct values and hasn't been affected by others
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);
        Assert.Equal(10, row0.Location);
        Assert.Equal(5, row0.SizeOrLength);
        Assert.Equal(3, row0.NumberOfRows);

        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);
        Assert.Equal(20, row1.Location);
        Assert.Equal(8, row1.SizeOrLength);
        Assert.Equal(1, row1.NumberOfRows);

        Assert.Equal(ElementTokenType.String, row2.TokenType);
        Assert.Equal(30, row2.Location);
        Assert.Equal(12, row2.SizeOrLength);
        Assert.Equal(1, row2.NumberOfRows);

        Assert.Equal(ElementTokenType.StartArray, row3.TokenType);
        Assert.Equal(40, row3.Location);
        Assert.Equal(15, row3.SizeOrLength);
        Assert.Equal(6, row3.NumberOfRows);

        Assert.Equal(ElementTokenType.Number, row4.TokenType);
        Assert.Equal(50, row4.Location);
        Assert.Equal(4, row4.SizeOrLength);
        Assert.Equal(1, row4.NumberOfRows);
    }

    [Fact]
    public void MetaDb_SetLocation_DoesNotAffectOtherRows()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 200, sizeOrLength: 20, rows: 1);
        var index2 = metaDb.Append(ElementTokenType.String, location: 300, sizeOrLength: 30, rows: 1);

        // Act - Modify location of middle row
        metaDb.SetLocation(index1, 999);

        // Assert - Verify only the target row changed
        var row0 = metaDb.Get(index0);
        Assert.Equal(100, row0.Location);
        Assert.Equal(10, row0.SizeOrLength);
        Assert.Equal(5, row0.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);

        var row1 = metaDb.Get(index1);
        Assert.Equal(999, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
        Assert.Equal(1, row1.NumberOfRows);
        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);

        var row2 = metaDb.Get(index2);
        Assert.Equal(300, row2.Location);
        Assert.Equal(30, row2.SizeOrLength);
        Assert.Equal(1, row2.NumberOfRows);
        Assert.Equal(ElementTokenType.String, row2.TokenType);
    }

    [Fact]
    public void MetaDb_SetSizeOrLength_DoesNotAffectOtherRows()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 200, sizeOrLength: 20, rows: 1);
        var index2 = metaDb.Append(ElementTokenType.String, location: 300, sizeOrLength: 30, rows: 1);

        // Act - Modify sizeOrLength of first row
        metaDb.SetSizeOrLength(index0, 777);

        // Assert - Verify only the target row changed
        var row0 = metaDb.Get(index0);
        Assert.Equal(100, row0.Location);
        Assert.Equal(777, row0.SizeOrLength);
        Assert.Equal(5, row0.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);

        var row1 = metaDb.Get(index1);
        Assert.Equal(200, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
        Assert.Equal(1, row1.NumberOfRows);
        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);

        var row2 = metaDb.Get(index2);
        Assert.Equal(300, row2.Location);
        Assert.Equal(30, row2.SizeOrLength);
        Assert.Equal(1, row2.NumberOfRows);
        Assert.Equal(ElementTokenType.String, row2.TokenType);
    }

    [Fact]
    public void MetaDb_SetRows_DoesNotAffectOtherRows()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 200, sizeOrLength: 20, rows: 1);
        var index2 = metaDb.Append(ElementTokenType.String, location: 300, sizeOrLength: 30, rows: 1);

        // Act - Modify rows of last row
        metaDb.SetRows(index2, 888);

        // Assert - Verify only the target row changed
        var row0 = metaDb.Get(index0);
        Assert.Equal(100, row0.Location);
        Assert.Equal(10, row0.SizeOrLength);
        Assert.Equal(5, row0.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);

        var row1 = metaDb.Get(index1);
        Assert.Equal(200, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
        Assert.Equal(1, row1.NumberOfRows);
        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);

        var row2 = metaDb.Get(index2);
        Assert.Equal(300, row2.Location);
        Assert.Equal(30, row2.SizeOrLength);
        Assert.Equal(888, row2.NumberOfRows);
        Assert.Equal(ElementTokenType.String, row2.TokenType);
    }

    [Fact]
    public void MetaDb_SetElementTokenType_DoesNotAffectOtherRows()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 200, sizeOrLength: 20, rows: 1);
        var index2 = metaDb.Append(ElementTokenType.String, location: 300, sizeOrLength: 30, rows: 1);

        // Act - Modify token type of middle row
        metaDb.SetElementTokenType(index1, ElementTokenType.Number);

        // Assert - Verify only the target row changed
        var row0 = metaDb.Get(index0);
        Assert.Equal(100, row0.Location);
        Assert.Equal(10, row0.SizeOrLength);
        Assert.Equal(5, row0.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row0.TokenType);

        var row1 = metaDb.Get(index1);
        Assert.Equal(200, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
        Assert.Equal(1, row1.NumberOfRows);
        Assert.Equal(ElementTokenType.Number, row1.TokenType);

        var row2 = metaDb.Get(index2);
        Assert.Equal(300, row2.Location);
        Assert.Equal(30, row2.SizeOrLength);
        Assert.Equal(1, row2.NumberOfRows);
        Assert.Equal(ElementTokenType.String, row2.TokenType);
    }

    [Fact]
    public void MetaDb_SetSizeOrLength_PreservesHasComplexChildrenFlag()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var indexWithFlag = metaDb.Append(
            ElementTokenType.StartObject,
            location: 100,
            sizeOrLength: 10,
            rows: 5,
            hasComplexChildren: true);
        var indexWithoutFlag = metaDb.Append(
            ElementTokenType.StartObject,
            location: 200,
            sizeOrLength: 20,
            rows: 3,
            hasComplexChildren: false);

        // Act - Update sizeOrLength for both rows
        metaDb.SetSizeOrLength(indexWithFlag, 50);
        metaDb.SetSizeOrLength(indexWithoutFlag, 60);

        // Assert - Verify flag is preserved
        var rowWithFlag = metaDb.Get(indexWithFlag);
        Assert.Equal(50, rowWithFlag.SizeOrLength);
        Assert.True(rowWithFlag.HasComplexChildren);
        Assert.Equal(5, rowWithFlag.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, rowWithFlag.TokenType);

        var rowWithoutFlag = metaDb.Get(indexWithoutFlag);
        Assert.Equal(60, rowWithoutFlag.SizeOrLength);
        Assert.False(rowWithoutFlag.HasComplexChildren);
        Assert.Equal(3, rowWithoutFlag.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, rowWithoutFlag.TokenType);
    }

    [Fact]
    public void MetaDb_SetRows_PreservesTokenType()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartArray, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.PropertyName, location: 200, sizeOrLength: 20, rows: 1);

        // Act - Update rows
        metaDb.SetRows(index0, 42);
        metaDb.SetRows(index1, 7);

        // Assert - Verify token types are preserved
        var row0 = metaDb.Get(index0);
        Assert.Equal(ElementTokenType.StartArray, row0.TokenType);
        Assert.Equal(42, row0.NumberOfRows);
        Assert.Equal(100, row0.Location);
        Assert.Equal(10, row0.SizeOrLength);

        var row1 = metaDb.Get(index1);
        Assert.Equal(ElementTokenType.PropertyName, row1.TokenType);
        Assert.Equal(7, row1.NumberOfRows);
        Assert.Equal(200, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
    }

    [Fact]
    public void MetaDb_SetElementTokenType_PreservesRowCount()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index0 = metaDb.Append(ElementTokenType.StartObject, location: 100, sizeOrLength: 10, rows: 5);
        var index1 = metaDb.Append(ElementTokenType.String, location: 200, sizeOrLength: 20, rows: 99);

        // Act - Update token types
        metaDb.SetElementTokenType(index0, ElementTokenType.StartArray);
        metaDb.SetElementTokenType(index1, ElementTokenType.Number);

        // Assert - Verify row counts are preserved
        var row0 = metaDb.Get(index0);
        Assert.Equal(ElementTokenType.StartArray, row0.TokenType);
        Assert.Equal(5, row0.NumberOfRows);
        Assert.Equal(100, row0.Location);
        Assert.Equal(10, row0.SizeOrLength);

        var row1 = metaDb.Get(index1);
        Assert.Equal(ElementTokenType.Number, row1.TokenType);
        Assert.Equal(99, row1.NumberOfRows);
        Assert.Equal(200, row1.Location);
        Assert.Equal(20, row1.SizeOrLength);
    }

    [Fact]
    public void MetaDb_MultipleUpdatesToSameRow_PreservesOtherFields()
    {
        // Arrange
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        var index = metaDb.Append(
            ElementTokenType.StartObject,
            location: 100,
            sizeOrLength: 10,
            rows: 5,
            hasComplexChildren: true);

        // Act - Update different fields multiple times
        metaDb.SetLocation(index, 200);
        var row1 = metaDb.Get(index);

        metaDb.SetSizeOrLength(index, 20);
        var row2 = metaDb.Get(index);

        metaDb.SetRows(index, 7);
        var row3 = metaDb.Get(index);

        metaDb.SetElementTokenType(index, ElementTokenType.StartArray);
        var row4 = metaDb.Get(index);

        // Assert - Each update preserves other fields
        Assert.Equal(200, row1.Location);
        Assert.Equal(10, row1.SizeOrLength);
        Assert.Equal(5, row1.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row1.TokenType);
        Assert.True(row1.HasComplexChildren);

        Assert.Equal(200, row2.Location);
        Assert.Equal(20, row2.SizeOrLength);
        Assert.Equal(5, row2.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row2.TokenType);
        Assert.True(row2.HasComplexChildren);

        Assert.Equal(200, row3.Location);
        Assert.Equal(20, row3.SizeOrLength);
        Assert.Equal(7, row3.NumberOfRows);
        Assert.Equal(ElementTokenType.StartObject, row3.TokenType);
        Assert.True(row3.HasComplexChildren);

        Assert.Equal(200, row4.Location);
        Assert.Equal(20, row4.SizeOrLength);
        Assert.Equal(7, row4.NumberOfRows);
        Assert.Equal(ElementTokenType.StartArray, row4.TokenType);
        Assert.True(row4.HasComplexChildren);
    }

    [Fact]
    public void MetaDb_ManyRows_MaintainDataIsolation()
    {
        // Arrange - Create many rows
        var metaDb = new SourceResultDocumentBuilder.MetaDb();
        const int rowCount = 100;
        var indices = new int[rowCount];

        for (var i = 0; i < rowCount; i++)
        {
            indices[i] = metaDb.Append(
                ElementTokenType.StartObject,
                location: i * 100,
                sizeOrLength: i * 10,
                rows: i + 1);
        }

        // Act - Modify every 10th row
        for (var i = 0; i < rowCount; i += 10)
        {
            metaDb.SetLocation(indices[i], 9999);
            metaDb.SetSizeOrLength(indices[i], 8888);
            metaDb.SetRows(indices[i], 7777);
            metaDb.SetElementTokenType(indices[i], ElementTokenType.StartArray);
        }

        // Assert - Verify modified and unmodified rows
        for (var i = 0; i < rowCount; i++)
        {
            var row = metaDb.Get(indices[i]);

            if (i % 10 == 0)
            {
                // Modified rows
                Assert.Equal(9999, row.Location);
                Assert.Equal(8888, row.SizeOrLength);
                Assert.Equal(7777, row.NumberOfRows);
                Assert.Equal(ElementTokenType.StartArray, row.TokenType);
            }
            else
            {
                // Unmodified rows
                Assert.Equal(i * 100, row.Location);
                Assert.Equal(i * 10, row.SizeOrLength);
                Assert.Equal(i + 1, row.NumberOfRows);
                Assert.Equal(ElementTokenType.StartObject, row.TokenType);
            }
        }
    }

    private Operation CreateOperation(string query)
    {
        var schema = ComposeShoppingSchema();
        var compiler = PlanOperation(schema, query);
        return compiler.Operation;
    }
}
