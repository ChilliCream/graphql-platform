using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldDescriptor
        : IDescriptor<SortFieldDefinition>
        , IFluent
    {
        ISortFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        ISortFieldDescriptor Name(NameString value);

        ISortFieldDescriptor Description(string value);

        ISortFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        ISortFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        ISortFieldDescriptor Type(ITypeNode typeNode);

        ISortFieldDescriptor Type(Type type);

        ISortFieldDescriptor Ignore(bool ignore = true);

        ISortFieldDescriptor DefaultValue(IValueNode value);

        ISortFieldDescriptor DefaultValue(object value);

        ISortFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        ISortFieldDescriptor Directive<T>()
            where T : class, new();

        ISortFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
