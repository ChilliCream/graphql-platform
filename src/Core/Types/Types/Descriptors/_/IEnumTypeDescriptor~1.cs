using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        new IEnumTypeDescriptor<T> SyntaxNode(EnumTypeDefinitionNode syntaxNode);

        new IEnumTypeDescriptor<T> Name(NameString name);

        new IEnumTypeDescriptor<T> Description(string description);

        IEnumValueDescriptor Item(T value);

        new IEnumTypeDescriptor<T> BindItems(BindingBehavior bindingBehavior);

        new IEnumTypeDescriptor<T> Directive<TDirective>(TDirective directive)
            where TDirective : class;

        new IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IEnumTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
