using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
    {
        public ObjectTypeDescriptor()
            : base(typeof(T))
        {
            ObjectDescription.ClrType = typeof(T);
        }

        protected void BindFields(BindingBehavior bindingBehavior)
        {
            ObjectDescription.FieldBindingBehavior = bindingBehavior;
        }

        protected override void OnCompleteFields(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (ObjectDescription.FieldBindingBehavior ==
                BindingBehavior.Implicit)
            {
                AddImplicitFields(fields, handledMembers);
            }

            AddResolverTypes(fields);
        }

        private void AddImplicitFields(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            foreach (KeyValuePair<MemberInfo, string> member in
                GetAllMembers(handledMembers))
            {
                if (!fields.ContainsKey(member.Value))
                {
                    var fieldDescriptor = new ObjectFieldDescriptor(
                        member.Key,
                        ObjectDescription.ClrType);

                    fields[member.Value] = fieldDescriptor
                        .CreateDescription();
                }
            }
        }

        private Dictionary<MemberInfo, string> GetAllMembers(
            ISet<MemberInfo> handledMembers)
        {
            var members = new Dictionary<MemberInfo, string>();

            foreach (KeyValuePair<string, MemberInfo> member in
                ReflectionUtils.GetMembers(ObjectDescription.ClrType))
            {
                if (!handledMembers.Contains(member.Value))
                {
                    members[member.Value] = member.Key;
                }
            }

            return members;
        }

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.BindFields(
            BindingBehavior bindingBehavior)
        {
            BindFields(bindingBehavior);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>
            .Interface<TInterface>()
        {
            Interface<TInterface>();
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>
            .Interface<TInterface>(TInterface type)
        {
            Interface<TInterface>(type);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Include<TResolver>()
        {
            Include(typeof(TResolver));
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.IsOfType(
            IsOfType isOfType)
        {
            IsOfType(isOfType);
            return this;
        }

        IObjectFieldDescriptor IObjectTypeDescriptor<T>.Field(
            Expression<Func<T, object>> propertyOrMethod)
        {
            return Field(propertyOrMethod);
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive<TDirective>(
            TDirective directive)
        {
            ObjectDescription.Directives.AddDirective(directive);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>
            .Directive<TDirective>()
        {
            ObjectDescription.Directives.AddDirective(new TDirective());
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
