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

        IInputObjectTypeDescriptor Name(NameString name);

        IInputObjectTypeDescriptor Description(string description);

        IInputFieldDescriptor Field(NameString name);
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
    }
}
