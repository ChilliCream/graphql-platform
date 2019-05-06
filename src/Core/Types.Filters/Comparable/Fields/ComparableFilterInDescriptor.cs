using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Comparable.Fields
{
    public class ComparableFilterInDescriptor : FilterFieldDescriptor, IComparableFilterFieldDetailsDescriptor
    {
        private readonly IComparableFilterFieldDescriptor parent;

        public ComparableFilterInDescriptor(IComparableFilterFieldDescriptor parent, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            this.parent = parent;
            Definition.Type = GetTypeReference();
            Definition.Name += "_in"; 
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

        protected override ITypeReference GetTypeReference()
        {
            ITypeReference baseType = base.GetTypeReference();
            if (baseType is ClrTypeReference type)
            {
                Type listType = typeof(IEnumerable<>).MakeGenericType(type.Type);
                return new ClrTypeReference(listType, type.Context, true, true);

            }
            throw new ArgumentException("Definition has no valid Type");
        }

    }
}
