using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor<T>
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IInputObjectTypeDescriptor<T> SyntaxNode(
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode);

        IInputObjectTypeDescriptor<T> Name(NameString value);

        IInputObjectTypeDescriptor<T> Description(string value);

        IInputObjectTypeDescriptor<T> BindFields(
            BindingBehavior behavior);

        /// <summary>
        /// Defines that all fields have to be specified explicitly.
        /// </summary>
        IInputObjectTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// Defines that all fields shall be infered
        /// from the associated .Net type,
        /// </summary>
        IInputObjectTypeDescriptor<T> BindFieldsImplicitly();

        IInputFieldDescriptor Field(NameString name);

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
