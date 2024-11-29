using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FieldSelectionMergingRuleTests()
    : DocumentValidatorVisitorTestBase(builder => builder.AddFieldRules())
{
    [Fact]
    public void MergeIdenticalFields()
    {
        ExpectValid(
            """
            {
                dog {
                    ... mergeIdenticalFields
                }
            }

            fragment mergeIdenticalFields on Dog {
                name
                name
            }
            """);
    }

    [Fact]
    public void MergeIdenticalAliasesAndFields()
    {
        ExpectValid(
            """
            {
                dog {
                    ... mergeIdenticalAliasesAndFields
                }
            }

            fragment mergeIdenticalAliasesAndFields on Dog {
                otherName: name
                otherName: name
            }
            """);
    }

    [Fact]
    public void ConflictingBecauseAlias()
    {
        ExpectErrors(
            """
            {
                dog {
                    ... conflictingBecauseAlias
                }
            }

            fragment conflictingBecauseAlias on Dog {
                name: nickname
                name
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void MergeIdenticalFieldsWithIdenticalArgs()
    {
        ExpectValid(
            """
            {
                dog {
                    ... mergeIdenticalFieldsWithIdenticalArgs
                }
            }

            fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: SIT)
            }
            """);
    }

    [Fact]
    public void MergeIdenticalFieldsWithIdenticalValues()
    {
        ExpectValid(
            """
            {
                dog {
                    ... mergeIdenticalFieldsWithIdenticalValues
                }
            }

            fragment mergeIdenticalFieldsWithIdenticalValues on Dog {
                doesKnowCommand(dogCommand: $dogCommand)
                doesKnowCommand(dogCommand: $dogCommand)
            }
            """);
    }

    [Fact]
    public void ConflictingArgsOnValues()
    {
        ExpectErrors(
            """
            {
                dog {
                    ... conflictingArgsOnValues
                }
            }

            fragment conflictingArgsOnValues on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: HEEL)
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void ConflictingArgsValueAndVar()
    {
        ExpectErrors(
            """
            query($dogCommand: DogCommand!) {
                dog {
                    ... conflictingArgsValueAndVar
                }
            }

            fragment conflictingArgsValueAndVar on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: $dogCommand)
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void ConflictingArgsWithVars()
    {
        ExpectErrors(
            """
            query($varOne: DogCommand! $varTwo: DogCommand!) {
                dog {
                    ... conflictingArgsWithVars
                }
            }

            fragment conflictingArgsWithVars on Dog {
                doesKnowCommand(dogCommand: $varOne)
                doesKnowCommand(dogCommand: $varTwo)
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void DifferingArgs()
    {
        ExpectErrors(
            """
            {
                dog {
                    ... differingArgs
                }
            }

            fragment differingArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void SameResponseNameDifferentFieldName()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... dog
                }
                catOrDog: dogOrHuman {
                    ... dog
                }
            }

            fragment dog on Dog {
                doesKnowCommand
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void SafeDifferingFields()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... safeDifferingFields
                }
            }

            fragment safeDifferingFields on Pet {
                ... on Dog {
                    volume: barkVolume
                }
                ... on Cat {
                    volume: meowVolume
                }
            }
            """);
    }

    [Fact]
    public void SafeDifferingArgs()
    {
        ExpectValid(
            """
            {
                dog {
                    ... safeDifferingArgs
                }
            }

            fragment safeDifferingArgs on Pet {
                ... on Dog {
                    doesKnowCommand(dogCommand: SIT)
                }
                ... on Cat {
                    doesKnowCommand(catCommand: JUMP)
                }
            }
            """);
    }

    [Fact]
    public void ConflictingDifferingResponses()
    {
        ExpectErrors(
            """
            {
                dog {
                    ... conflictingDifferingResponses
                }
            }

            fragment conflictingDifferingResponses on Pet {
                ... on Dog {
                    someValue: nickname
                }
                ... on Cat {
                    someValue: meowVolume
                }
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void ShortHandQueryWithNoDuplicateFields()
    {
        ExpectValid(
            """
            {
                __type (type: "Foo") {
                    name
                    fields {
                        name
                        type {
                            name
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void Stream_Mergeable()
    {
        ExpectValid(
            """
            {
                __type (type: "Foo") {
                    name
                    fields @stream(initialCount: 1) {
                        type {
                            name
                        }
                    }
                    fields @stream(initialCount: 1) {
                        name
                    }
                }
            }
            """);
    }

    [Fact]
    public void Stream_Argument_Mismatch()
    {
        ExpectErrors(
            """
            {
                __type (type: "Foo") {
                    name
                    fields @stream(initialCount: 1) {
                        type {
                            name
                        }
                    }
                    fields @stream(initialCount: 2) {
                        name
                    }
                }
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void Stream_On_Some_Fields()
    {
        ExpectErrors(
            """
            {
                __type (type: "Foo") {
                    name
                    fields @stream(initialCount: 1) {
                        type {
                            name
                        }
                    }
                    fields {
                        name
                    }
                }
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void ShortHandQueryWithDuplicateFieldInSecondLevelFragment()
    {
        ExpectErrors(
            """
            {
                dog {
                    doesKnowCommand(dogCommand: DOWN)
                    ... FooLevel1
                }
            }

            fragment FooLevel1 on Dog {
                ... FooLevel2
            }

            fragment FooLevel2 on Dog {
                doesKnowCommand(dogCommand: HEEL)
            }
            """,
            t => Assert.Equal(
                "Encountered fields for the same object that cannot be merged.",
                t.Message));
    }

    [Fact]
    public void ShortHandQueryWithDupMergeableFieldInSecondLevelFragment()
    {
        // arrange
        ExpectValid(
            """
            {
                dog {
                    doesKnowCommand(dogCommand: DOWN)
                    ... FooLevel1
                }
            }

            fragment FooLevel1 on Dog {
                ... FooLevel2
            }

            fragment FooLevel2 on Dog {
                doesKnowCommand(dogCommand: DOWN)
            }
            """);
    }

    [Fact]
    public void TypeNameFieldOnInterfaceIsMergeable()
    {
        // arrange
        ExpectValid(
            """
            {
                dog {
                    ... interfaceFieldSelection
                }
            }

            fragment interfaceFieldSelection on Pet {
                __typename
                __typename
            }
            """);
    }

    [Fact]
    public void TypeNameFieldOnUnionIsMergeable()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... interfaceFieldSelection
                }
            }

            fragment interfaceFieldSelection on CatOrDog {
                __typename
                __typename
            }
            """);
    }

    [Fact]
    public void TypeNameFieldOnObjectIsMergeable()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... interfaceFieldSelection
                }
            }

            fragment interfaceFieldSelection on Cat {
                __typename
                __typename
            }
            """);
    }

    [Fact]
    public void InvalidFieldsShouldNotRaiseValidationError()
        => ExpectValid(FileResource.Open("InvalidIntrospectionQuery.graphql"));

    [Fact]
    public void UniqueFields()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... uniqueFields
                }
            }

            fragment uniqueFields on Dog {
                name
                nickname
            }
            """);
    }

    [Fact]
    public void IdenticalFields()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... mergeIdenticalFields
                }
            }

            fragment mergeIdenticalFields on Dog {
                name
                name
            }
            """);
    }

    [Fact]
    public void IdenticalFieldsWithIdenticalArgs()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... mergeIdenticalFieldsWithIdenticalArgs
                }
            }

            fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: SIT)
            }
            """);
    }

    [Fact]
    public void DifferentArgsWithDifferentAliases()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... differentArgsWithDifferentAliases
                }
            }

            fragment differentArgsWithDifferentAliases on Dog {
                knowsSit: doesKnowCommand(dogCommand: SIT)
                knowsDown: doesKnowCommand(dogCommand: DOWN)
            }
            """);
    }

    [Fact]
    public void DifferentDirectivesWithDifferentAliases()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... differentDirectivesWithDifferentAliases
                }
            }

            fragment differentDirectivesWithDifferentAliases on Dog {
                nameIfTrue: name @include(if: true)
                nameIfFalse: name @include(if: false)
            }
            """);
    }

    [Fact]
    public void DifferentSkipIncludeDirectivesAccepted()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... differentDirectivesWithDifferentAliases
                }
            }

            fragment differentDirectivesWithDifferentAliases on Dog {
                name @include(if: true)
                name @include(if: false)
            }
            """);
    }

    [Fact]
    public void SameAliasesWithDifferentFieldTargets()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... sameAliasesWithDifferentFieldTargets
                }
            }

            fragment sameAliasesWithDifferentFieldTargets on Dog {
                fido: name
                fido: nickname
            }
            """);
    }

    [Fact]
    public void SameAliasesAllowedOnNonOverlappingFields()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... sameAliasesWithDifferentFieldTargets
                }
            }

            fragment sameAliasesWithDifferentFieldTargets on Pet {
                ... on Dog {
                    name
                }
                ... on Cat {
                    name: nickname
                }
            }
            """);
    }

    [Fact]
    public void AliasMaskingDirectFieldAccess()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... aliasMaskingDirectFieldAccess
                }
            }

            fragment aliasMaskingDirectFieldAccess on Dog {
                name: nickname
                name
            }
            """);
    }

    [Fact]
    public void DifferentArgsSecondAddsAnArgument()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... conflictingArgs
                }
            }
            fragment conflictingArgs on Dog {
                doesKnowCommand
                doesKnowCommand(dogCommand: HEEL)
            }
            """);
    }

    [Fact]
    public void DifferentArgsSecondMissingAnArgument()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... conflictingArgs
                }
            }

            fragment conflictingArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand
            }
            """);
    }

    [Fact]
    public void ConflictingArgValues()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... conflictingArgs
                }
            }

            fragment conflictingArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: HEEL)
            }
            """);
    }

    [Fact]
    public void ConflictingArgNames()
    {
        ExpectErrors(
            """
            {
                catOrDog {
                    ... conflictingArgs
                }
            }

            fragment conflictingArgs on Dog {
                isAtLocation(x: 0)
                isAtLocation(y: 0)
            }
            """);
    }

    [Fact]
    public void AllowsDifferentArgsWhereNoConflictIsPossible()
    {
        ExpectValid(
            """
            {
                catOrDog {
                    ... conflictingArgs
                }
            }

            fragment conflictingArgs on Pet {
                ... on Dog {
                name(surname: true)
                }
                ... on Cat {
                name
                }
            }
            """);
    }

    [Fact]
    public void EncountersConflictInFragments()
    {
        ExpectErrors(
            """
            {
                ...A
                ...B
            }

            fragment A on Query {
                x: a
            }

            fragment B on Query {
                x: b
            }
            """);
    }

    [Fact]
    public void ReportsEachConflictOnce()
    {
        ExpectErrors(
            """
            {
                f1 {
                    ...A
                    ...B
                }
                f2 {
                    ...B
                    ...A
                }
                f3 {
                    ...A
                    ...B
                    x: c
                }
            }

            fragment A on Query {
                x: a
            }

            fragment B on Query {
                x: b
            }
            """);
    }

    [Fact]
    public void DeepConflict()
    {
        ExpectErrors(
            """
            {
                f1 {
                    x: a
                }
                f1 {
                    x: b
                }
            }
            """);
    }

    [Fact]
    public void DeepConflictWithMultipleIssues()
    {
        ExpectErrors(
            """
            {
                f1 {
                    x: a
                    y: c
                },
                f1 {
                    x: b
                    y: d
                }
            }
            """);
    }

    [Fact]
    public void VeryDeepConflict()
    {
        ExpectErrors(
            """
            {
                f1 {
                    f2 {
                        x: a
                    }
                }
                f1 {
                    f2 {
                        x: b
                    }
                }
            }
            """);
    }

    [Fact]
    public void ReportsDeepConflictToNearestCommonAncestor()
    {
        ExpectErrors(
            """
            {
                f1 {
                    f2 {
                        x: a
                    }
                    f2 {
                        x: b
                    }
                }
                f1 {
                    f2 {
                        y
                    }
                }
            }
            """);
    }

    [Fact]
    public void ReportsDeepConflictToNearestCommonAncestorInFragments()
    {
        ExpectErrors(
            """
            {
                f1 {
                    ...F
                }
                f1 {
                    ...F
                }
            }
            fragment F on Query {
                f2 {
                    f3 {
                        x: a
                    }
                    f3 {
                        x: b
                    }
                }
                f2 {
                    f3 {
                        y
                    }
                }
            }
            """);
    }

    [Fact]
    public void ReportsDeepConflictInNestedFragments()
    {
        ExpectErrors(
            """
            {
                f1 {
                    ...F
                }
                f1 {
                    ...I
                }
            }

            fragment F on Query {
                x: a
                ...G
            }

            fragment G on Query {
                y: c
            }

            fragment I on Query {
                y: d
                ...J
            }

            fragment J on Query {
                x: b
            }
            """);
    }

    [Fact]
    public void ConflictingReturnTypesWhichPotentiallyOverlap()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ...on IntBox {
                        scalar
                    }
                    ...on NonNullStringBox1 {
                        scalar
                    }
                }
            }
            """);
    }

    [Fact]
    public void CompatibleReturnShapesOnDifferentReturnTypes()
    {
        ExpectValid(
            TestSchema,
            """
            {
                someBox {
                    ... on SomeBox {
                        deepBox {
                            unrelatedField
                        }
                    }
                    ... on StringBox {
                        deepBox {
                            unrelatedField
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void DisallowsDifferingReturnTypesDespiteNoOverlap()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        scalar
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """);
    }

    [Fact]
    public void DisallowsDifferingReturnTypeNullabilityDespiteNoOverlap()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on NonNullStringBox1 {
                        scalar
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """);
    }

    [Fact]
    public void DisallowsDifferingReturnTypeListDespiteNoOverlap()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        box: listStringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: stringBox {
                            scalar
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void DisallowsDifferingReturnTypeListDespiteNoOverlapReverse()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: listStringBox {
                            scalar
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void DisallowsDifferingSubfields()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            val: scalar
                            val: unrelatedField
                        }
                    }
                    ... on StringBox {
                        box: stringBox {
                            val: scalar
                        }
                    }
                }
            }
            """);
    }

    // TODO : Fix this issue
    [Fact(Skip = "This one needs fixing!")]
    public void DisallowsDifferingDeepReturnTypesDespiteNoOverlap()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: intBox {
                            scalar
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public void AllowsNonConflictingOverlappingTypes()
    {
        ExpectValid(
            TestSchema,
            """
            {
                someBox {
                    ... on IntBox {
                        scalar: unrelatedField
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """);
    }

    // TODO : we need to analyze this validation issue further.
    [Fact(Skip = "This one needs to be analyzed further.")]
    public void SameWrappedScalarReturnTypes()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                someBox {
                    ...on NonNullStringBox1 {
                        scalar
                    }
                    ...on NonNullStringBox2 {
                        scalar
                    }
                }
            }
            """);
    }

    [Fact]
    public void AllowsInlineFragmentsWithoutTypeCondition()
    {
        ExpectValid(
            TestSchema,
            """
            {
                a
                ... {
                    a
                }
            }
            """);
    }

    [Fact]
    public void ComparesDeepTypesIncludingList()
    {
        ExpectErrors(
            TestSchema,
            """
            {
                connection {
                    ...edgeID
                    edges {
                        node {
                            id: name
                        }
                    }
                }
            }

            fragment edgeID on Connection {
                edges {
                    node {
                        id
                    }
                }
            }
            """);
    }

    [Fact]
    public void FindsInvalidCaseEvenWithImmediatelyRecursiveFragment()
    {
        ExpectErrors(
            """
            {
                dogOrHuman {
                    ... sameAliasesWithDifferentFieldTargets
                }
            }

            fragment sameAliasesWithDifferentFieldTargets on Dog {
                ...sameAliasesWithDifferentFieldTargets
                fido: name
                fido: nickname
            }
            """);
    }

    private static readonly ISchema TestSchema =
        SchemaBuilder.New()
            .AddDocumentFromString(
                """
                interface SomeBox {
                    deepBox: SomeBox
                    unrelatedField: String
                }

                type StringBox implements SomeBox {
                    scalar: String
                    deepBox: StringBox
                    unrelatedField: String
                    listStringBox: [StringBox]
                    stringBox: StringBox
                    intBox: IntBox
                }

                type IntBox implements SomeBox {
                    scalar: Int
                    deepBox: IntBox
                    unrelatedField: String
                    listStringBox: [StringBox]
                    stringBox: StringBox
                    intBox: IntBox
                }

                interface NonNullStringBox1 {
                    scalar: String!
                }

                type NonNullStringBox1Impl implements SomeBox & NonNullStringBox1 {
                    scalar: String!
                    unrelatedField: String
                    deepBox: SomeBox
                }

                interface NonNullStringBox2 {
                    scalar: String!
                }

                type NonNullStringBox2Impl implements SomeBox & NonNullStringBox2 {
                    scalar: String!
                    unrelatedField: String
                    deepBox: SomeBox
                }

                type Connection {
                    edges: [Edge]
                }

                type Edge {
                    node: Node
                }

                type Node {
                    id: ID
                    name: String
                }

                type Query {
                    someBox: SomeBox
                    connection: Connection
                    a: String
                    d: String
                    y: String
                }
                """)
            .AddResolver("StringBox", "deepBox", () => "")
            .AddResolver("StringBox", "intBox", () => "")
            .AddResolver("StringBox", "listStringBox", () => "")
            .AddResolver("StringBox", "scalar", () => "")
            .AddResolver("StringBox", "stringBox", () => "")
            .AddResolver("StringBox", "unrelatedField", () => "")
            .AddResolver("IntBox", "deepBox", () => "")
            .AddResolver("IntBox", "intBox", () => "")
            .AddResolver("IntBox", "listStringBox", () => "")
            .AddResolver("IntBox", "scalar", () => "")
            .AddResolver("IntBox", "stringBox", () => "")
            .AddResolver("IntBox", "unrelatedField", () => "")
            .AddResolver("NonNullStringBox1Impl", "deepBox", () => "")
            .AddResolver("NonNullStringBox1Impl", "scalar", () => "")
            .AddResolver("NonNullStringBox1Impl", "unrelatedField", () => "")
            .AddResolver("NonNullStringBox2Impl", "deepBox", () => "")
            .AddResolver("NonNullStringBox2Impl", "scalar", () => "")
            .AddResolver("NonNullStringBox2Impl", "unrelatedField", () => "")
            .AddResolver("Connection", "edges", () => "")
            .AddResolver("Edge", "node", () => "")
            .AddResolver("Node", "id", () => "")
            .AddResolver("Node", "name", () => "")
            .AddResolver("Query", "connection", () => "")
            .AddResolver("Query", "someBox", () => "")
            .AddResolver("Query", "a", () => "")
            .AddResolver("Query", "d", () => "")
            .AddResolver("Query", "y", () => "")
            .AddType(new AnyType())
            .Create();
}
