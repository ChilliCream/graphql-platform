using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterOperationFieldDescriptor
        : IDescriptor<FilterOperationFieldDefinition>
        , IFluent
    {
        IFilterOperationFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        IFilterOperationFieldDescriptor Name(NameString value);

        IFilterOperationFieldDescriptor Description(string value);

        IFilterOperationFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IFilterOperationFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IFilterOperationFieldDescriptor Type(ITypeNode typeNode);

        IFilterOperationFieldDescriptor Type(Type type);

        IFilterOperationFieldDescriptor Ignore(bool ignore = true);

        IFilterOperationFieldDescriptor DefaultValue(IValueNode value);

        IFilterOperationFieldDescriptor DefaultValue(object value);

        IFilterOperationFieldDescriptor Operation(int operation);

        IFilterOperationFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IFilterOperationFieldDescriptor Directive<T>()
            where T : class, new();

        IFilterOperationFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
