using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class CustomFilterFieldDescriptor
       : FilterFieldDescriptorBase,
       ICustomFilterFieldDescriptor
    {
        private readonly CustomFilterOperationDescriptor _fieldDefinition;

        public CustomFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(FilterKind.Custom, context, property, filterConventions)
        {
            var operation = new FilterOperation(
                property.PropertyType,
                Definition.Kind,
                FilterOperationKind.Custom,
                Definition.Property);

            _fieldDefinition = CustomFilterOperationDescriptor.New(
                Context,
                CreateFieldName(FilterOperationKind.Custom),
                RewriteType(FilterOperationKind.Custom),
                operation,
                FilterConvention);

            Filters.Add(_fieldDefinition);
        }

        public CustomFilterFieldDescriptor(
            IDescriptorContext context,
            NameString name,
            IFilterConvention filterConventions)
            : base(FilterKind.Custom, context, filterConventions)
        {
            var operation = new FilterOperation(
                typeof(object),
                Definition.Kind,
                FilterOperationKind.Custom,
                Definition.Property);

            _fieldDefinition = CustomFilterOperationDescriptor.New(
                Context,
                name,
                null,
                operation,
                FilterConvention);

            Filters.Add(_fieldDefinition);
        }

        public ICustomFilterFieldDescriptor DefaultValue(IValueNode value)
        {
            _fieldDefinition.DefaultValue(value);
            return this;
        }

        public ICustomFilterFieldDescriptor DefaultValue(object value)
        {
            _fieldDefinition.DefaultValue(value);
            return this;
        }

        public ICustomFilterFieldDescriptor Description(string value)
        {
            _fieldDefinition.Description(value);
            return this;
        }

        public ICustomFilterFieldDescriptor Directive<T>(T directiveInstance) where T : class
        {
            _fieldDefinition.Directive(directiveInstance);
            return this;
        }

        public ICustomFilterFieldDescriptor Directive<T>() where T : class, new()
        {
            _fieldDefinition.Directive<T>();
            return this;
        }

        public ICustomFilterFieldDescriptor Directive(
            NameString name, params ArgumentNode[] arguments)
        {
            _fieldDefinition.Directive(name, arguments);
            return this;
        }

        public ICustomFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = true;
            return this;
        }

        public ICustomFilterFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode)
        {
            _fieldDefinition.SyntaxNode(inputValueDefinitionNode);
            return this;
        }

        public new ICustomFilterFieldDescriptor Name(NameString value)
        {
            _fieldDefinition.Name(value);
            return this;
        }

        public new ICustomFilterFieldDescriptor Type(ITypeNode typeNode)
        {
            _fieldDefinition.Type(typeNode);
            return this;
        }

        public new ICustomFilterFieldDescriptor Type(Type type)
        {
            _fieldDefinition.Type(type);
            return this;
        }

        public new ICustomFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            _fieldDefinition.Type<TInputType>();
            return this;
        }

        public new ICustomFilterFieldDescriptor Type<TInputType>(TInputType inputType)
           where TInputType : class, IInputType
        {
            _fieldDefinition.Type(inputType);
            return this;
        }
        public ICustomFilterFieldDescriptor OperationKind(int kind)
        {
            _fieldDefinition.WithOperationKind(kind);
            return this;
        }

        public ICustomFilterFieldDescriptor Kind(int kind)
        {
            _fieldDefinition.WithFilterKind(kind);
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            int operationKind)
        {
            throw new NotSupportedException();
        }

        public static CustomFilterFieldDescriptor New(
            IDescriptorContext context,
            IFilterConvention filterConventions,
            NameString name) =>
            new CustomFilterFieldDescriptor(context, name, filterConventions);
    }
}