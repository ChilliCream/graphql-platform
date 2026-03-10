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
    [Fact]
    public void SplitCompositeKey()
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
                    keyField1: Int!
                }
                """,
                """
                # Schema C
                type Query {
                    productByKeyField1(keyField1: Int!): Product @lookup
                }

                type Product {
                    keyField2: Int!
                }
                """,
                """
                # Schema D
                type Query {
                    productByKey(keyField1: Int!, keyField2: Int!): Product @lookup
                }

                type Product {
                    keyField1: Int!
                    keyField2: Int!
                    specialField: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var queryType = (MutableObjectTypeDefinition)schema.Types["Query"];
        var productByIdField = queryType.Fields["productById"];
        var requirementsValidator = new RequirementsValidator(schema);
        var selectedValue = new FieldSelectionMapParser("specialField").Parse();
        var selectionSet =
            new ValueSelectionToSelectionSetRewriter(schema).Rewrite(
                selectedValue,
                productByIdField.Type.AsTypeDefinition());
        var parentPathItem = new SatisfiabilityPathItem(productByIdField, queryType, "A");

        // act
        var errors =
            requirementsValidator.Validate(
                selectionSet,
                (MutableObjectTypeDefinition)productByIdField.Type,
                parentPathItem,
                excludeSchemaName: "A");

        // assert
        Assert.Empty(errors);
    }
}
