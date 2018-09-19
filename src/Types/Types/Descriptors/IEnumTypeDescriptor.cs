using System.Collections.Generic;
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

        IEnumTypeDescriptor Directive<T>(T directive)
            where T : class;

        IEnumTypeDescriptor Directive<T>()
            where T : class, new();

        IEnumTypeDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }

    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        new IEnumTypeDescriptor<T> SyntaxNode(EnumTypeDefinitionNode syntaxNode);

        new IEnumTypeDescriptor<T> Name(string name);

        new IEnumTypeDescriptor<T> Description(string description);

        IEnumValueDescriptor Item(T value);

        new IEnumTypeDescriptor<T> BindItems(BindingBehavior bindingBehavior);

        new IEnumTypeDescriptor<T> Directive<TDirective>(TDirective directive)
            where TDirective : class;

        new IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IEnumTypeDescriptor<T> Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
