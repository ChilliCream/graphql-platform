using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityRequireInlineFragmentTests
{
    [Fact]
    public void Validate_Should_Succeed_When_RequireSelectsFieldsOfEachUnionMember()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    shipments: [Shipment]
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel
                }

                union Parcel = Box | Envelope

                type Box {
                    volume: Int
                }

                type Envelope {
                    sheets: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel @external
                    postage(
                        volume: Int @require(field: "parcel<Box>.volume")
                        sheets: Int @require(field: "parcel<Envelope>.sheets")
                    ): Int
                }

                union Parcel = Box | Envelope

                type Box {
                    volume: Int @external
                }

                type Envelope {
                    sheets: Int @external
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_Should_Succeed_When_RequireSelectsFieldOfInaccessibleUnionMember(
        bool inaccessibleInRequiringSchema)
    {
        // arrange
        var (owningEnvelopeDirectives, requiringEnvelopeDirectives) = inaccessibleInRequiringSchema
            ? ("", " @inaccessible")
            : (" @inaccessible", "");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                $$"""
                type Query {
                    shipments: [Shipment]
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel
                }

                union Parcel = Box | Envelope

                type Box {
                    volume: Int
                }

                type Envelope{{owningEnvelopeDirectives}} {
                    sheets: Int
                }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel @external
                    postage(
                        volume: Int @require(field: "parcel<Box>.volume")
                        sheets: Int @require(field: "parcel<Envelope>.sheets")
                    ): Int
                }

                union Parcel = Box | Envelope

                type Box {
                    volume: Int @external
                }

                type Envelope{{requiringEnvelopeDirectives}} {
                    sheets: Int @external
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequireSelectsUnreachableUnionMemberField()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    shipments: [Shipment]
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel
                }

                union Parcel = Box | Envelope

                type Box {
                    label: String @shareable
                }

                type Envelope {
                    sheets: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    parcel: Parcel @external
                    postage(volume: Int @require(field: "parcel<Box>.volume")): Int
                }

                union Parcel = Box | Envelope

                type Box {
                    volume: Int @external
                }

                type Envelope {
                    sheets: Int @external
                }
                """),
            new SourceSchemaText(
                "C",
                """
                type Query {
                    boxes: [Box]
                }

                type Box {
                    label: String @shareable
                    volume: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Shipment.postage' on path 'A:Query.shipments<Shipment>'.
              Unable to satisfy the requirement '{ parcel { ... on Box { volume } } }' on field 'B:Shipment.postage<Int>'.
                Unable to satisfy the requirement 'parcel { ... on Box { volume } }'.
                  Unable to access the required field 'Shipment.parcel' on path 'A:Query.shipments<Shipment>'.
                    Unable to access the required field 'Box.volume' on path 'A:Query.shipments<Shipment> -> A:Shipment.parcel<Parcel>'.
                      Unable to transition between schemas 'A' and 'C' for access to required field 'C:Box.volume<Int>'.
                        No lookups found for type 'Box' in schema 'C'.

            Unable to access the field 'Box.volume' on path 'A:Query.shipments<Shipment> -> A:Shipment.parcel<Parcel>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:Box.volume<Int>'.
                No lookups found for type 'Box' in schema 'C'.
            """);
    }
}
