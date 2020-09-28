using HotChocolate.Language;

namespace HotChocolate.Data.Sorting
{
    public interface ISortEnumValueDescriptor
    {
        ISortEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition);

        ISortEnumValueDescriptor Name(NameString value);

        ISortEnumValueDescriptor Description(string value);

        ISortEnumValueDescriptor Deprecated(string reason);

        ISortEnumValueDescriptor Deprecated();

        ISortEnumValueDescriptor Directive<T>(T directiveInstance)
            where T : class;

        ISortEnumValueDescriptor Directive<T>()
            where T : class, new();

        ISortEnumValueDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
