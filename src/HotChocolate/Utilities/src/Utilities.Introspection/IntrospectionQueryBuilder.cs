using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// A utility to build the GraphQL introspection request document.
/// </summary>
internal static class IntrospectionQueryBuilder
{
    public static DocumentNode Build(ServerCapabilities features, IntrospectionOptions options)
    {
        var selections = new List<ISelectionNode>();

        if (features.HasSchemaDescription)
        {
            selections.Add(new FieldNode("description"));
        }

        selections.Add(
            new FieldNode(
                new NameNode("queryType"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FieldNode("name"),
                    })));

        selections.Add(
            new FieldNode(
                new NameNode("mutationType"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FieldNode("name"),
                    })));

        if (features.HasSubscriptionSupport)
        {
            selections.Add(
                new FieldNode(
                    new NameNode("subscriptionType"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    new SelectionSetNode(
                        new ISelectionNode[]
                        {
                            new FieldNode("name"),
                        })));
        }

        selections.Add(CreateTypesField());

        selections.Add(
            CreateDirectivesField(
                features.HasDirectiveLocations,
                features.HasRepeatableDirectives));

        return new DocumentNode(
            new IDefinitionNode[]
            {
                new OperationDefinitionNode(
                    null,
                    new NameNode("IntrospectionQuery"),
                    OperationType.Query,
                    Array.Empty<VariableDefinitionNode>(),
                    Array.Empty<DirectiveNode>(),
                    new SelectionSetNode(
                        new ISelectionNode[]
                        {
                            new FieldNode(
                                new NameNode("__schema"),
                                null,
                                Array.Empty<DirectiveNode>(),
                                Array.Empty<ArgumentNode>(),
                                new SelectionSetNode(selections)),
                        })),
                BuildFullTypeFragment(features.HasArgumentDeprecation),
                BuildInputValueFragment(),
                BuildTypeRefFragment(options.TypeDepth),
            });
    }

    private static FieldNode CreateTypesField()
        => new FieldNode(
            new NameNode("types"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FragmentSpreadNode(
                        null,
                        new NameNode("FullType"),
                        Array.Empty<DirectiveNode>()),
                }));

    private static FieldNode CreateDirectivesField(bool hasLocationsField, bool hasRepeatableDirective)
    {
        var selections = new List<ISelectionNode>
        {
            new FieldNode("name"),
            new FieldNode("description"),
            new FieldNode(
                new NameNode("args"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FragmentSpreadNode(
                            null,
                            new NameNode("InputValue"),
                            Array.Empty<DirectiveNode>()),
                    })),
        };

        if (hasLocationsField)
        {
            selections.Add(new FieldNode("locations"));
        }
        else
        {
            selections.Add(new FieldNode("onOperation"));
            selections.Add(new FieldNode("onFragment"));
            selections.Add(new FieldNode("onField"));
        }

        if (hasRepeatableDirective)
        {
            selections.Add(new FieldNode("isRepeatable"));
        }

        return new FieldNode(
            new NameNode("directives"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(selections));
    }

    private static FragmentDefinitionNode BuildFullTypeFragment(bool includeDeprecatedArgs)
        => new FragmentDefinitionNode(
            null,
            new NameNode("FullType"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("__Type"),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FieldNode("kind"),
                    new FieldNode("name"),
                    new FieldNode("description"),
                    CreateFields(includeDeprecatedArgs),
                    CreateInputFields(includeDeprecatedArgs),
                    CreateInterfacesField(),
                    CreateEnumValuesField(),
                    CreatePossibleTypesField(),
                }));

    private static FieldNode CreateFields(bool includeDeprecatedArgs)
        => new FieldNode(
            new NameNode("fields"),
            null,
            Array.Empty<DirectiveNode>(),
            new[]
            {
                new ArgumentNode("includeDeprecated", true),
            },
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FieldNode("name"),
                    new FieldNode("description"),
                    CreateArgsField(includeDeprecatedArgs),
                    CreateTypeField(),
                    new FieldNode("isDeprecated"),
                    new FieldNode("deprecationReason"),
                }));

    private static FieldNode CreateArgsField(bool includeDeprecated)
        => includeDeprecated
            ? new FieldNode(
                new NameNode("args"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new ArgumentNode("includeDeprecated", true),
                },
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FragmentSpreadNode(
                            null,
                            new NameNode("InputValue"),
                            Array.Empty<DirectiveNode>()),
                        new FieldNode("isDeprecated"),
                        new FieldNode("deprecationReason"),
                    }))
            : new FieldNode(
                new NameNode("args"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FragmentSpreadNode(
                            null,
                            new NameNode("InputValue"),
                            Array.Empty<DirectiveNode>()),
                    }));

    private static FieldNode CreateTypeField()
        => new FieldNode(
            new NameNode("type"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FragmentSpreadNode(
                        null,
                        new NameNode("TypeRef"),
                        Array.Empty<DirectiveNode>()),
                }));

    private static FieldNode CreateInputFields(bool includeDeprecatedFields)
        => includeDeprecatedFields
            ? new FieldNode(
                new NameNode("inputFields"),
                null,
                Array.Empty<DirectiveNode>(),
                new[]
                {
                    new ArgumentNode("includeDeprecated", true),
                },
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FragmentSpreadNode(
                            null,
                            new NameNode("InputValue"),
                            Array.Empty<DirectiveNode>()),
                        new FieldNode("isDeprecated"),
                        new FieldNode("deprecationReason"),
                    }))
            : new FieldNode(
                new NameNode("inputFields"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(
                    new ISelectionNode[]
                    {
                        new FragmentSpreadNode(
                            null,
                            new NameNode("InputValue"),
                            Array.Empty<DirectiveNode>()),
                    }));

    private static FieldNode CreateInterfacesField()
        => new FieldNode(
            new NameNode("interfaces"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FragmentSpreadNode(
                        null,
                        new NameNode("TypeRef"),
                        Array.Empty<DirectiveNode>()),
                }));

    private static FieldNode CreateEnumValuesField()
        => new FieldNode(
            new NameNode("enumValues"),
            null,
            Array.Empty<DirectiveNode>(),
            new[]
            {
                new ArgumentNode("includeDeprecated", true),
            },
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FieldNode("name"),
                    new FieldNode("description"),
                    new FieldNode("isDeprecated"),
                    new FieldNode("deprecationReason"),
                }));

    private static FieldNode CreatePossibleTypesField()
        => new FieldNode(
            new NameNode("possibleTypes"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FragmentSpreadNode(
                        null,
                        new NameNode("TypeRef"),
                        Array.Empty<DirectiveNode>()),
                }));

    private static FragmentDefinitionNode BuildInputValueFragment()
        => Utf8GraphQLParser.Syntax.ParseFragmentDefinition(
            """
            fragment InputValue on __InputValue {
              name
              description
              type {
                ...TypeRef
              }
              defaultValue
            }
            """);

    private static FragmentDefinitionNode BuildTypeRefFragment(int depth)
    {
        depth -= 2;

        var ofType = CreateOfType();

        while (depth > 0)
        {
            ofType = CreateOfType(ofType);
            depth--;
        }

        return new FragmentDefinitionNode(
            null,
            new NameNode("TypeRef"),
            Array.Empty<VariableDefinitionNode>(),
            new NamedTypeNode("__Type"),
            Array.Empty<DirectiveNode>(),
            CreateKindTypePair(ofType));
    }

    private static FieldNode CreateOfType(FieldNode? ofType = null)
        => new(
            new NameNode("ofType"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            CreateKindTypePair(ofType));

    private static SelectionSetNode CreateKindTypePair(FieldNode? ofType)
        => ofType is null
            ? new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FieldNode(
                        new NameNode("kind"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null),
                    new FieldNode(
                        new NameNode("name"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null),
                })
            : new SelectionSetNode(
                new ISelectionNode[]
                {
                    new FieldNode(
                        new NameNode("kind"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null),
                    new FieldNode(
                        new NameNode("name"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null),
                    ofType,
                });
}
