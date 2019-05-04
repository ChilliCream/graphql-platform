using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.String.Contains;

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
            throw new NotImplementedException();
        }

        public IStringFilterFieldDetailsDescriptor AllowIn()
        {
            throw new NotImplementedException();
        }

        public IStringFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            this.Definition.Filters.BindingBehavior = bindingBehavior;
            return this;
        }
    }
}
