using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Stitching.Types;

internal sealed class ObjectTypeDefinition : ITypeDefinition<ObjectTypeDefinitionNode>
{
    public ObjectTypeDefinition(ObjectTypeDefinitionNode definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public string Name => Definition.Name.Value;

    public TypeKind Kind => TypeKind.Object;

    public bool IsExtension => false;

    public ObjectTypeDefinitionNode Definition { get; set; }

    public BindingList Bindings { get; } = new();

    public static bool TryParse(
        string s,
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
            definition = new ObjectTypeDefinition(definitionNode);
            return true;
        }
        catch (SyntaxException)
        {
            definition = null;
            return false;
        }
    }
}
