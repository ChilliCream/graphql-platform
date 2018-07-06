using System;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor
        : IFluent
    {
        IInputObjectTypeDescriptor SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode);

        IInputObjectTypeDescriptor Name(string name);

        IInputObjectTypeDescriptor Description(string description);

        IInputFieldDescriptor Field(string name);
    }

    public interface IInputObjectTypeDescriptor<T>
        : IInputObjectTypeDescriptor
    {
        new IInputObjectTypeDescriptor<T> SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode);

        new IInputObjectTypeDescriptor<T> Name(string name);

        new IInputObjectTypeDescriptor<T> Description(string description);

        IInputObjectTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

        IInputFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> property);
    }
}
