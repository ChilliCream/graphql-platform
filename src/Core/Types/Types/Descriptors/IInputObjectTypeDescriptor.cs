using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor
        : IFluent
    {
        IInputObjectTypeDescriptor SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode);

        IInputObjectTypeDescriptor Name(NameString name);

        IInputObjectTypeDescriptor Description(string description);

        IInputFieldDescriptor Field(NameString name);

        IInputObjectTypeDescriptor Directive<T>(T directive)
            where T : class;

        IInputObjectTypeDescriptor Directive<T>()
            where T : class, new();

        IInputObjectTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IInputObjectTypeDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }

    public interface IInputObjectTypeDescriptor<T>
        : IInputObjectTypeDescriptor
    {
        new IInputObjectTypeDescriptor<T> SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode);

        new IInputObjectTypeDescriptor<T> Name(NameString name);

        new IInputObjectTypeDescriptor<T> Description(string description);

        IInputObjectTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior);

        IInputFieldDescriptor Field<TValue>(
            Expression<Func<T, TValue>> property);

        new IInputObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        new IInputObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IInputObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

        new IInputObjectTypeDescriptor<T> Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
