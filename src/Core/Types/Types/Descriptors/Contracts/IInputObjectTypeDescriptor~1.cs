using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor<T>
        : IFluent
    {
        IInputObjectTypeDescriptor<T> SyntaxNode(
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode);

        IInputObjectTypeDescriptor<T> Name(NameString value);

        IInputObjectTypeDescriptor<T> Description(string value);

        IInputObjectTypeDescriptor<T> BindFields(
            BindingBehavior behavior);

        IInputFieldDescriptor Field<TValue>(
            Expression<Func<T, TValue>> property);

        IInputObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IInputObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IInputObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
