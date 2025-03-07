namespace HotChocolate.Types;

public static class EmptyCollections
{
    public static IReadOnlyDirectiveCollection Directives
        => EmptyDirectiveCollection.Instance;

    public static IReadOnlyInterfaceTypeDefinitionCollection InterfaceTypeDefinitions
        => EmptyInterfaceTypeDefinitionCollection.Instance;

    public static IReadOnlyFieldDefinitionCollection<IInputValueDefinition> InputFieldDefinitions
        => EmptyInputFieldDefinitionCollection.Instance;

    public static IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> OutputFieldDefinitions
        => EmptyOutputFieldDefinitionCollection.Instance;
}
