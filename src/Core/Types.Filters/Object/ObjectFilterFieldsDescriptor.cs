using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Object.Fields;

namespace HotChocolate.Types.Filters.Object
{
    public class ObjectFilterFieldsDescriptor : FilterFieldDescriptorBase, IObjectFilterFieldDescriptor
    {
        private readonly Type objectType;

        private PropertyInfo propertyInfo { get; }

        public ObjectFilterFieldsDescriptor(Type objectType, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            this.objectType = objectType;
            propertyInfo = property;
            
        }


        public new IObjectFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        public IObjectFilterFieldDetailsDescriptor AllowObject()
        {
            var field = new ObjectFilterEqualsDescriptor(objectType, this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }
    }
}
