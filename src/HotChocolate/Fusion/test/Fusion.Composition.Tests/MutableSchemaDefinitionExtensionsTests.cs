using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

public sealed class MutableSchemaDefinitionExtensionsTests
{
    [Fact]
    public void RemoveUnreferencedDefinitions_Should_KeepUnionAndMember_When_SeedUnionsAsRootsTrue()
    {
        // arrange
        var schema = CreateSchemaWithUnreferencedUnion();

        // act
        schema.RemoveUnreferencedDefinitions(new HashSet<string>(), seedUnionsAsRoots: true);

        // assert
        Assert.True(schema.Types.ContainsName("Media"));
        Assert.True(schema.Types.ContainsName("Audio"));
    }

    [Fact]
    public void RemoveUnreferencedDefinitions_Should_PruneUnionAndMember_When_SeedUnionsAsRootsFalse()
    {
        // arrange
        var schema = CreateSchemaWithUnreferencedUnion();

        // act
        schema.RemoveUnreferencedDefinitions(new HashSet<string>(), seedUnionsAsRoots: false);

        // assert
        Assert.False(schema.Types.ContainsName("Media"));
        Assert.False(schema.Types.ContainsName("Audio"));
    }

    private static MutableSchemaDefinition CreateSchemaWithUnreferencedUnion()
    {
        var stringType = BuiltIns.String.Create();
        var queryType = new MutableObjectTypeDefinition(Query);
        queryType.Fields.Add(
            new MutableOutputFieldDefinition("ping", stringType)
            {
                DeclaringMember = queryType
            });

        var objectType = new MutableObjectTypeDefinition("Audio");
        objectType.Fields.Add(
            new MutableOutputFieldDefinition("id", stringType)
            {
                DeclaringMember = objectType
            });

        var unionType = new MutableUnionTypeDefinition("Media");
        unionType.Types.Add(objectType);

        return new MutableSchemaDefinition
        {
            QueryType = queryType,
            Types =
            {
                queryType,
                unionType,
                objectType
            }
        };
    }
}
