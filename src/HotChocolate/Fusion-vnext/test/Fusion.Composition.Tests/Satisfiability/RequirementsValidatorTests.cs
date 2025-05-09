using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class RequirementsValidatorTests
{
//     [Fact]
//     public void X()
//     {
//         // arrange
//         var merger = new SourceSchemaMerger(
//             CreateSchemaDefinitions(
//             [
//                 """
//                 # Schema A
//                 type Query {
//                     productById(id: ID!): Product @lookup
//                 }
//
//                 type Product {
//                     id: ID!
//                 }
//                 """,
//                 """
//                 # Schema B
//                 type Query {
//                     productById(id: ID!): Product @lookup
//                 }
//
//                 type Product {
//                     id: ID!
//                     name: String
//                 }
//                 """
//             ]),
//             new SourceSchemaMergerOptions { AddFusionDefinitions = false });
//
//         var schema = merger.Merge().Value;
//         var requirementsValidator = new RequirementsValidator(schema);
//         var selectedValue = new FieldSelectionMapParser("id").Parse();
//         var satisfiabilityPath = new SatisfiabilityPath();
//         var queryType = (MutableObjectTypeDefinition)schema.Types["Query"];
//         var productByIdField = queryType.Fields["productById"];
//         satisfiabilityPath.Push(new SatisfiabilityPathItem(productByIdField, queryType, "A"));
//
//         // act
//         var result =
//             requirementsValidator.Validate(
//                 selectedValue,
//                 RequirementKind.Lookup,
//                 satisfiabilityPath);
//
//         // assert
//         //Assert.True(result.IsSuccess);
//     }

    [Fact]
    public void Y()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    keyField1: KeyField!
                }

                type KeyField {
                    #id: ID!
                    id: IdType!
                }

                type IdType {
                    fld: ID!
                }
                """,
                """
                # Schema C
                type Query {
                    #productByKeyField1(keyField1: KeyField!): Product @lookup
                    productByKeyField1(keyField1: ID! @is(field: "keyField1.id.fld")): Product @lookup
                }

                type Product {
                    keyField2: Int!
                }
                """,
                """
                # Schema D
                type Query {
                    #productByKey(keyField1: KeyField!, keyField2: Int!): Product @lookup
                    productByKey(keyField1: ID! @is(field: "keyField1.id.fld"), keyField2: Int!): Product @lookup
                }

                type Product {
                    keyField1: KeyField!
                    keyField2: Int!
                    specialField: Int!
                }

                type KeyField {
                    #id: ID!
                    id: IdType!
                }

                type IdType {
                    fld: ID!
                    fld2: ID!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var queryType = (MutableObjectTypeDefinition)schema.Types["Query"];
        var productByIdField = queryType.Fields["productById"];
        var requirementsValidator = new RequirementsValidator(schema);
        //var selectedValue = new FieldSelectionMapParser("{ a: keyField1.id.fld b: keyField2 }").Parse(); //
        var selectedValue = new FieldSelectionMapParser("{ a: keyField1.id.{fld fld2} }").Parse(); // b: keyField2
        //var selectedValue = new FieldSelectionMapParser("{ a: keyField1.id b: keyField2 }").Parse(); //
        var selectionSet = new SelectedValueToSelectionSetRewriter(schema).SelectedValueToSelectionSet(selectedValue, productByIdField.Type.AsTypeDefinition());
        var parentPath = new SatisfiabilityPath();
        parentPath.Push(new SatisfiabilityPathItem(productByIdField, queryType, "A"));

        // act
        var errors =
            requirementsValidator.Validate(
                selectionSet,
                RequirementKind.Lookup,
                (MutableObjectTypeDefinition)productByIdField.Type,//tmp test
                parentPath,
                excludeSchemaName: "X"); //?

        // assert
        Assert.Empty(errors);
    }
}
