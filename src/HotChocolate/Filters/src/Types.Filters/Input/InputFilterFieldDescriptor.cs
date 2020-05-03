using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class InputFilterFieldDescriptor
       : FilterFieldDescriptorBase,
       IInputFilterFieldDescriptor
    {
        private readonly InputFilterOperationDescriptor _fieldDefinition;

        public InputFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(FilterKind.Skip, context, property, filterConventions)
        {
            var operation = new FilterOperation(
                property.PropertyType,
                Definition.Kind,
                FilterOperationKind.Skip,
                Definition.Property);

            _fieldDefinition = InputFilterOperationDescriptor.New(
                Context,
                CreateFieldName(FilterOperationKind.Skip),
                RewriteType(FilterOperationKind.Skip),
                operation,
                FilterConvention);

            Filters.Add(_fieldDefinition);
        }

        public IInputFilterFieldDescriptor DefaultValue(IValueNode value)
        {
            _fieldDefinition.DefaultValue(value);
            return this;
        }

        public IInputFilterFieldDescriptor DefaultValue(object value)
        {
            _fieldDefinition.DefaultValue(value);
            return this;
        }

        public IInputFilterFieldDescriptor Description(string value)
        {
            _fieldDefinition.Description(value);
            return this;
        }

        public IInputFilterFieldDescriptor Directive<T>(T directiveInstance) where T : class
        {
            _fieldDefinition.Directive(directiveInstance);
            return this;
        }

        public IInputFilterFieldDescriptor Directive<T>() where T : class, new()
        {
            _fieldDefinition.Directive<T>();
            return this;
        }

        public IInputFilterFieldDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        {
            _fieldDefinition.Directive(name, arguments);
            return this;
        }

        public IInputFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = true;
            return this;
        }

        public IInputFilterFieldDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinitionNode)
        {
            _fieldDefinition.SyntaxNode(inputValueDefinitionNode);
            return this;
        }

        public new IInputFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IInputFilterFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IInputFilterFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public new IInputFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IInputFilterFieldDescriptor Type<TInputType>(TInputType inputType)
           where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind)
        {
            throw new NotSupportedException();
        }
    }
}