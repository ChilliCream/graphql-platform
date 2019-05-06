using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Object.Fields
{
    public class ObjectFilterEqualsDescriptor : FilterFieldDescriptor, IObjectFilterFieldDetailsDescriptor
    {
        private readonly Type objectType;
        private readonly IObjectFilterFieldDescriptor parent;

        public ObjectFilterEqualsDescriptor(Type objectType, IObjectFilterFieldDescriptor parent, IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
            this.objectType = objectType;
            this.parent = parent;
            Definition.Type = GetTypeReference(); 
            Definition.Property = null;
        }

        public IObjectFilterFieldDescriptor And()
        {
            return parent;
        }

        public new IObjectFilterFieldDetailsDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IObjectFilterFieldDetailsDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IObjectFilterFieldDetailsDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IObjectFilterFieldDetailsDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IObjectFilterFieldDetailsDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        protected override ITypeReference GetTypeReference()
        {
            if (Definition.Type is ClrTypeReference type)
            {
                return new ClrTypeReference(objectType, type.Context, true, true);

            }
            throw new ArgumentException("Definition has no valid Type");
        }

    }
}
