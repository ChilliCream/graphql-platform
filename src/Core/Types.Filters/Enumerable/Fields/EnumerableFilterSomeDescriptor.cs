using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Enumerable.Fields
{
    public class EnumerableFilterSomeDescriptor : FilterFieldDescriptor, IEnumerableFilterFieldDetailsDescriptor
    {
        private readonly ITypeReference typeReference;
        private readonly IEnumerableFilterFieldDescriptor parent;

        public EnumerableFilterSomeDescriptor(ITypeReference typeReference, IEnumerableFilterFieldDescriptor parent, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            this.typeReference = typeReference;
            this.parent = parent;
            Definition.Name += "_some";
            Definition.Type = GetTypeReference(); 
            Definition.Property = null;
        }

        public IEnumerableFilterFieldDescriptor And()
        {
            return parent;
        }

        public new IEnumerableFilterFieldDetailsDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IEnumerableFilterFieldDetailsDescriptor Directive<TDirective>(TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IEnumerableFilterFieldDetailsDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IEnumerableFilterFieldDetailsDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IEnumerableFilterFieldDetailsDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        protected override ITypeReference GetTypeReference()
        {
            return typeReference;
        }

    }
}
