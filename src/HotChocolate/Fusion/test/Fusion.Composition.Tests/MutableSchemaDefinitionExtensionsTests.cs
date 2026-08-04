using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

public sealed class MutableSchemaDefinitionExtensionsTests
{
    [Fact]
    public void RemoveUnreferencedDefinitions_Should_PruneDeadExternalField_When_ItsCarrierIsRemoved()
    {
        // arrange
        // 'Carrier' is unreachable, so it is pruned; its @provides was the only reference to
        // 'Entity.external', which is therefore dead and must be pruned in the same pass.
        var schema = SchemaParser.Parse(
            """
            directive @external on FIELD_DEFINITION
            directive @provides(fields: String!) on FIELD_DEFINITION

            type Query {
                entity: Entity
            }

            type Entity {
                id: ID
                external: String @external
            }

            type Carrier {
                link: Entity @provides(fields: "external")
            }
            """);

        // act
        schema.RemoveUnreferencedDefinitions(new HashSet<string>(), seedUnionsAsRoots: true);

        // assert
        Assert.False(schema.Types.ContainsName("Carrier"));
        Assert.True(schema.Types.TryGetType<MutableObjectTypeDefinition>("Entity", out var entity));
        Assert.False(entity.Fields.TryGetField("external", out _));
    }

    [Fact]
    public void RemoveUnreferencedDefinitions_Should_KeepExternalField_When_ReferencedBySurvivingProvides()
    {
        // arrange
        // 'Carrier' is reachable through Query, so its @provides survives and keeps
        // 'Entity.external' referenced; the external field must not be pruned.
        var schema = SchemaParser.Parse(
            """
            directive @external on FIELD_DEFINITION
            directive @provides(fields: String!) on FIELD_DEFINITION

            type Query {
                entity: Entity
                carrier: Carrier
            }

            type Entity {
                id: ID
                external: String @external
            }

            type Carrier {
                link: Entity @provides(fields: "external")
            }
            """);

        // act
        schema.RemoveUnreferencedDefinitions(new HashSet<string>(), seedUnionsAsRoots: true);

        // assert
        Assert.True(schema.Types.ContainsName("Carrier"));
        Assert.True(schema.Types.TryGetType<MutableObjectTypeDefinition>("Entity", out var entity));
        Assert.True(entity.Fields.TryGetField("external", out _));
    }

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
