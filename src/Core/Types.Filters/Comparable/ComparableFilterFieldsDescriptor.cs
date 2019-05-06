using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Comparable.Fields;

namespace HotChocolate.Types.Filters.Comparable
{
    public class ComparableFilterFieldsDescriptor : FilterFieldDescriptorBase, IComparableFilterFieldDescriptor
    {
        private PropertyInfo propertyInfo { get; }

        public ComparableFilterFieldsDescriptor(IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            propertyInfo = property;
            
        }
         

        public IComparableFilterFieldDetailsDescriptor AllowEquals()
        {
            var field = new ComparableFilterEqualsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }


        public IComparableFilterFieldDetailsDescriptor AllowIn()
        {
            var field = new ComparableFilterInDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IComparableFilterFieldDetailsDescriptor AllowGreaterThan()
        {
            var field = new ComparableFilterGreaterThanDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IComparableFilterFieldDetailsDescriptor AllowGreaterThanOrEquals()
        {
            var field = new ComparableFilterGreaterThanOrEqualsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IComparableFilterFieldDetailsDescriptor AllowLowerThan()
        {
            var field = new ComparableFilterLowerThanDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IComparableFilterFieldDetailsDescriptor AllowLowerThanOrEquals()
        {
            var field = new ComparableFilterLowerThanOrEqualsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }


        public new IComparableFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }
    }
}
