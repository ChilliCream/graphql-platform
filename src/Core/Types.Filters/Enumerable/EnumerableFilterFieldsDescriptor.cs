using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Enumerable.Fields; 

namespace HotChocolate.Types.Filters.IEnumerable
{
    public class EnumerableFilterFieldsDescriptor : FilterFieldDescriptorBase, IEnumerableFilterFieldDescriptor
    {
        private readonly ITypeReference referenceType;

        private PropertyInfo propertyInfo { get; }

        //TODO: this api can be simplified probably
        public EnumerableFilterFieldsDescriptor(IType type, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            referenceType = new SchemaTypeReference(new ListType(type), true, true);
            propertyInfo = property;
        }

        public EnumerableFilterFieldsDescriptor(ITypeReference typeReference, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            referenceType = typeReference;
            propertyInfo = property;
        }


        public new IEnumerableFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        public IEnumerableFilterFieldDetailsDescriptor AllowSome()
        { 
            var field = new EnumerableFilterSomeDescriptor(referenceType, this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }
    }
}
