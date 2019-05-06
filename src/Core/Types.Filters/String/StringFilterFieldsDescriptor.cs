using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Comparable.Fields;
using HotChocolate.Types.Filters.String.Fields;

namespace HotChocolate.Types.Filters.String
{
    public class StringFilterFieldsDescriptor : FilterFieldDescriptorBase, IStringFilterFieldDescriptor
    {
        private PropertyInfo propertyInfo { get; }

        public StringFilterFieldsDescriptor(IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            propertyInfo = property;
        }


        public IStringFilterFieldDetailsDescriptor AllowContains()
        {
           
            var field = new StringFilterContainsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
            
        }

        public IStringFilterFieldDetailsDescriptor AllowEquals()
        {
            var field = new StringFilterEqualsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IStringFilterFieldDetailsDescriptor AllowIn()
        {
            var field = new StringFilterInDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }
        public IStringFilterFieldDetailsDescriptor AllowStartsWith()
        {
            var field = new StringFilterStartsWithDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IStringFilterFieldDetailsDescriptor AllowEndsWith()
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
}
