using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class InputObjectTypeDescriptor<T>
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
            if (Definition.ClrType != typeof(object))
            {
                foreach (PropertyInfo property in
                    Context.Inspector.GetMembers(Definition.ClrType)
                        .OfType<PropertyInfo>())
                {
                    InputFieldDefinition fieldDefinition =
                        InputFieldDescriptor
                            .New(Context, property)
                            .CreateDefinition();

                    if (!handledProperties.Contains(property)
                        && !fields.ContainsKey(fieldDefinition.Name))
                    {
                        handledProperties.Add(property);
                        fields[fieldDefinition.Name] = fieldDefinition;
                    }
                }
            }
        }

        public new IInputObjectTypeDescriptor<T> SyntaxNode(
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode)
        {
            base.SyntaxNode(inputObjectTypeDefinitionNode);
            return this;
        }

        public new IInputObjectTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IInputObjectTypeDescriptor<T> Description(string value)
        {
            Description(value);
            return this;
        }

        public IInputObjectTypeDescriptor<T> BindFields(
            BindingBehavior behavior)
        {
            Definition.FieldBindingBehavior = behavior;
            return this;
        }

        public IInputFieldDescriptor Field<TValue>(
            Expression<Func<T, TValue>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new InputFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public new IInputObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            base.Directive<TDirective>(directive);
            return this;
        }

        public new IInputObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>(new TDirective());
            return this;
        }

        public new IInputObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
