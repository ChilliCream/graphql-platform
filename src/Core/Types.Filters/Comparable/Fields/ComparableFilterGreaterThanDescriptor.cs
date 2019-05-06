using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Comparable.Fields
{
    public class ComparableFilterGreaterThanDescriptor : FilterFieldDescriptor, IComparableFilterFieldDetailsDescriptor
    {
        private readonly IComparableFilterFieldDescriptor parent;

        public ComparableFilterGreaterThanDescriptor(IComparableFilterFieldDescriptor parent, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            this.parent = parent;
            Definition.Type = GetTypeReference();
            Definition.Name += "_gt"; 
            Definition.Property = null;
        }

        public IComparableFilterFieldDescriptor And()
        {
            return parent;
        }

        public new IComparableFilterFieldDetailsDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IComparableFilterFieldDetailsDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IComparableFilterFieldDetailsDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IComparableFilterFieldDetailsDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IComparableFilterFieldDetailsDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }
    }
}
