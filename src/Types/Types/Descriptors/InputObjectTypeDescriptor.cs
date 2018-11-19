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

        protected virtual void CompleteFields()
        {
            foreach (InputFieldDescriptor fieldDescriptor in Fields)
            {
                ObjectDescription.Fields.Add(
                    fieldDescriptor.CreateDescription());
            }
        }

        protected void SyntaxNode(InputObjectTypeDefinitionNode syntaxNode)
        {
            ObjectDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_Cannot_BeEmpty(),
                    nameof(name));
            }

            ObjectDescription.Name = name;
        }

        protected void Description(string description)
        {
            ObjectDescription.Description = description;
        }

        protected InputFieldDescriptor Field(NameString name)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_Cannot_BeEmpty(),
                    nameof(name));
            }

            var field = new InputFieldDescriptor(name);
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

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Description(string description)
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
        public InputObjectTypeDescriptor(Type clrType)
        {
            ObjectDescription.ClrType = clrType
                ?? throw new ArgumentNullException(nameof(clrType));
            ObjectDescription.Name = clrType.GetGraphQLName();
            ObjectDescription.Description = clrType.GetGraphQLDescription();

            // this convention will fix most type colisions where the
            // .net type is and input and an output type.
            // It is still possible to opt out via the descriptor.Name("Foo").
            if (!ObjectDescription.Name.EndsWith("Input"))
            {
                ObjectDescription.Name = ObjectDescription.Name + "Input";
            }
        }

        protected override void CompleteFields()
        {
            base.CompleteFields();

            var descriptions = new Dictionary<string, InputFieldDescription>();

            foreach (InputFieldDescription description in ObjectDescription.Fields)
            {
                descriptions[description.Name] = description;
            }

            if (ObjectDescription.FieldBindingBehavior == BindingBehavior.Implicit)
            {
                DeriveFieldsFromType(descriptions);
                ObjectDescription.Fields = descriptions.Values.ToList();
            }
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

        private void DeriveFieldsFromType(
                Dictionary<string, InputFieldDescription> descriptions)
        {
            Dictionary<PropertyInfo, string> properties =
                GetProperties(ObjectDescription.ClrType);

            foreach (InputFieldDescription description in descriptions.Values
                .Where(t => t.Property != null))
            {
                properties.Remove(description.Property);
            }

            foreach (KeyValuePair<PropertyInfo, string> property in properties)
            {
                if (!descriptions.ContainsKey(property.Value))
                {
                    var descriptor = new InputFieldDescriptor(property.Key);
                    descriptions[property.Value] = descriptor
                        .CreateDescription();
                }
            }
        }

        private static Dictionary<PropertyInfo, string> GetProperties(Type type)
        {
            var properties = new Dictionary<PropertyInfo, string>();

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(t => t.DeclaringType != typeof(object)))
            {
                properties[property] = property.GetGraphQLName();
            }

            return properties;
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
