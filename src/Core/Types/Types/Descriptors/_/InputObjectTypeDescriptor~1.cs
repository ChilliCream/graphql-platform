using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class InputObjectTypeDescriptor<T>
      : InputObjectTypeDescriptor
      , IInputObjectTypeDescriptor<T>
    {
        public InputObjectTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, InputFieldDefinition> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                AddImplicitFields(fields, handledProperties);
            }

            base.OnCompleteFields(fields, handledProperties);
        }

        private void AddImplicitFields(
            IDictionary<NameString, InputFieldDefinition> fields,
            ISet<PropertyInfo> handledProperties)
        {
            foreach (KeyValuePair<PropertyInfo, string> property in
                GetProperties(handledProperties))
            {
                if (!fields.ContainsKey(property.Value))
                {
                    var fieldDescriptor =
                        new InputFieldDescriptor(property.Key);

                    fields[property.Value] = fieldDescriptor
                        .CreateDescription();
                }
            }
        }

        private Dictionary<PropertyInfo, string> GetProperties(
            ISet<PropertyInfo> handledProperties)
        {
            var properties = new Dictionary<PropertyInfo, string>();

            foreach (KeyValuePair<string, PropertyInfo> property in
                ReflectionUtils.GetProperties(ObjectDescription.ClrType))
            {
                if (!handledProperties.Contains(property.Value))
                {
                    properties[property.Value] = property.Key;
                }
            }

            return properties;
        }

        protected void BindFields(BindingBehavior bindingBehavior)
        {
            ObjectDescription.FieldBindingBehavior = bindingBehavior;
        }

        protected InputFieldDescriptor Field<TValue>(
            Expression<Func<T, TValue>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new InputFieldDescriptor(p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        #region IInputObjectTypeDescriptor<T>

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.Name(
            NameString name)
        {
            Name(name);
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.BindFields(
            BindingBehavior bindingBehavior)
        {
            BindFields(bindingBehavior);
            return this;
        }

        IInputFieldDescriptor IInputObjectTypeDescriptor<T>.Field<TValue>(
            Expression<Func<T, TValue>> property)
        {
            return Field(property);
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>
            .Directive<TDirective>(TDirective directive)
        {
            ObjectDescription.Directives.AddDirective(directive);
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>
            .Directive<TDirective>()
        {
            ObjectDescription.Directives.AddDirective(new TDirective());
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
