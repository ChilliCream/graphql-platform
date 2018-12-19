using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputObjectTypeDescriptor
        : IInputObjectTypeDescriptor
        , IDescriptionFactory<InputObjectTypeDescription>
    {
        protected List<InputFieldDescriptor> Fields { get; } =
            new List<InputFieldDescriptor>();

        protected InputObjectTypeDescription ObjectDescription { get; } =
            new InputObjectTypeDescription();

        public InputObjectTypeDescription CreateDescription()
        {
            CompleteFields();
            return ObjectDescription;
        }

        private void CompleteFields()
        {
            var fields = new Dictionary<string, InputFieldDescription>();
            var handledProperties = new HashSet<PropertyInfo>();

            foreach (InputFieldDescriptor fieldDescriptor in Fields)
            {
                InputFieldDescription fieldDescription = fieldDescriptor
                    .CreateDescription();

                if (!fieldDescription.Ignored)
                {
                    fields[fieldDescription.Name] = fieldDescription;
                }

                if (fieldDescription.Property != null)
                {
                    handledProperties.Add(fieldDescription.Property);
                }
            }

            OnCompleteFields(fields, handledProperties);

            ObjectDescription.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<string, InputFieldDescription> fields,
            ISet<PropertyInfo> handledProperties)
        {
        }

        protected void SyntaxNode(InputObjectTypeDefinitionNode syntaxNode)
        {
            ObjectDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            ObjectDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            ObjectDescription.Description = description;
        }

        protected InputFieldDescriptor Field(NameString name)
        {
            var field = new InputFieldDescriptor(
                name.EnsureNotEmpty(nameof(name)));
            Fields.Add(field);
            return field;
        }

        #region IInputObjectTypeDescriptor

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.SyntaxNode(
            InputObjectTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Name(
            NameString name)
        {
            Name(name);
            return this;
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInputFieldDescriptor IInputObjectTypeDescriptor.Field(NameString name)
        {
            return Field(name);
        }

        #endregion
    }

    internal class InputObjectTypeDescriptor<T>
        : InputObjectTypeDescriptor
        , IInputObjectTypeDescriptor<T>
    {
        public InputObjectTypeDescriptor()
        {
            Type clrType = typeof(T);
            ObjectDescription.ClrType = clrType;
            ObjectDescription.Name = clrType.GetGraphQLName();
            ObjectDescription.Description = clrType.GetGraphQLDescription();

            // this convention will fix most type colisions where the
            // .net type is and input and an output type.
            // It is still possible to opt out via the descriptor.Name("Foo").
            if (!ObjectDescription.Name.EndsWith("Input",
                    StringComparison.Ordinal))
            {
                ObjectDescription.Name = ObjectDescription.Name + "Input";
            }
        }

        protected override void OnCompleteFields(
            IDictionary<string, InputFieldDescription> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (ObjectDescription.FieldBindingBehavior ==
                BindingBehavior.Implicit)
            {
                AddImplicitFields(fields, handledProperties);
            }
        }

        private void AddImplicitFields(
            IDictionary<string, InputFieldDescription> fields,
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

        #endregion
    }
}
