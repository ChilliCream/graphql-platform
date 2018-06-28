using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor
        : IFluent
    {
        IEnumTypeDescriptor SyntaxNode(EnumTypeDefinitionNode syntaxNode);

        IEnumTypeDescriptor Name(string name);

        IEnumTypeDescriptor Description(string description);

        IEnumValueDescriptor Item<T>(T value);

        IEnumTypeDescriptor BindItems(BindingBehavior bindingBehavior);
    }

    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        new IEnumTypeDescriptor<T> SyntaxNode(EnumTypeDefinitionNode syntaxNode);

        new IEnumTypeDescriptor<T> Name(string name);

        new IEnumTypeDescriptor<T> Description(string description);

        IEnumValueDescriptor Item(T value);

        new IEnumTypeDescriptor<T> BindItems(BindingBehavior bindingBehavior);
    }
}
