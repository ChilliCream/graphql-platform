using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Bindings;
using HotChocolate.Types;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Stitching.Types;

internal sealed class ObjectTypeDefinition : ITypeDefinition
{
    public ObjectTypeDefinition(ObjectTypeDefinitionNode definition, string schemaName)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Name = definition.Name.Value;

        foreach (FieldDefinitionNode field in Definition.Fields)
        {
            Bindings.Add(new SourceBinding(new(Name, field.Name.Value), schemaName));
        }
    }

    public string Name { get; }

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ObjectTypeDefinitionNode Definition { get; set; }

    IDefinitionNode ITypeDefinition.Definition => Definition;

    public BindingList Bindings { get; } = new();

    public static bool TryParse(
        string s,
        string schemaName,
        [NotNullWhen(true)] out ObjectTypeDefinition? definition)
    {
        if (string.IsNullOrEmpty(s))
        {
            definition = null;
            return false;
        }

        try
        {
            ObjectTypeDefinitionNode definitionNode = ParseObjectTypeDefinition(s);
            definition = new ObjectTypeDefinition(definitionNode, schemaName);
            return true;
        }
        catch (SyntaxException)
        {
            definition = null;
            return false;
        }
    }
}
