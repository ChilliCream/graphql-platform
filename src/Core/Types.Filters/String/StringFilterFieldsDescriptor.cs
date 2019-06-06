using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Comparable.Fields;
using HotChocolate.Types.Filters.String.Fields;

namespace HotChocolate.Types.Filters.String
{
    public class StringFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IStringFilterFieldDescriptor
    {
        private PropertyInfo propertyInfo { get; }

        public StringFilterFieldDescriptor(IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            propertyInfo = property;
        }


        public IStringFilterDescriptor AllowContains()
        {

            var field = new StringFilterContainsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;

        }

        public IStringFilterDescriptor AllowEquals()
        {
            var field = new StringFilterEqualsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IStringFilterDescriptor AllowIn()
        {
            var field = new StringFilterInDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }
        public IStringFilterDescriptor AllowStartsWith()
        {
            var field = new StringFilterStartsWithDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IStringFilterDescriptor AllowEndsWith()
        {
            var field = new StringFilterEndsWithDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

    }

    public class StringFilterDescriptor
        : FilterDescriptorBase
        , IStringFilterDescriptor
    {
        protected StringFilterDescriptor(
            IDescriptorContext context,
            FilterFieldDefintion fieldDefinition,
            StringFilterFieldDescriptor descriptor)
            : base(context, fieldDefinition)
        {
        }

        public IStringFilterFieldDescriptor And()
        {
            throw new NotImplementedException();
        }

        public IStringFilterDescriptor Name(NameString value)
        {
            throw new NotImplementedException();
        }

        IStringFilterDescriptor IStringFilterDescriptor.Description(string value)
        {
            throw new NotImplementedException();
        }

        IStringFilterDescriptor IStringFilterDescriptor.Directive<T>(T directiveInstance)
        {
            throw new NotImplementedException();
        }

        IStringFilterDescriptor IStringFilterDescriptor.Directive<T>()
        {
            throw new NotImplementedException();
        }

        IStringFilterDescriptor IStringFilterDescriptor.Directive(NameString name, params ArgumentNode[] arguments)
        {
            throw new NotImplementedException();
        }

        IDescriptorExtension<InputFieldDefinition> IDescriptor<InputFieldDefinition>.Extend()
        {
            throw new NotImplementedException();
        }

        public static StringFilterDescriptor New(
            IDescriptorContext context,
            FilterFieldDefintion fieldDefinition) =>
            new StringFilterDescriptor(context, fieldDefinition);
    }
}
