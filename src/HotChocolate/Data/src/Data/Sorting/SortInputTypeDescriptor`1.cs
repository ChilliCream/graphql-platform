using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting
{
    public class SortInputTypeDescriptor<T>
        : SortInputTypeDescriptor
        , ISortInputTypeDescriptor<T>
    {
        protected internal SortInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            string? scope)
            : base(context, entityType, scope)
        {
        }

        protected internal SortInputTypeDescriptor(
            IDescriptorContext context,
            string? scope)
            : base(context, scope)
        {
        }

        protected internal SortInputTypeDescriptor(
            IDescriptorContext context,
            SortInputTypeDefinition definition,
            string? scope)
            : base(context, definition, scope)
        {
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, SortFieldDefinition> fields,
            ISet<MemberInfo> handledProperties)
        {
            if (Definition.Fields.IsImplicitBinding() &&
                Definition.EntityType is {})
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    Definition.EntityType,
                    p => SortFieldDescriptor
                        .New(Context, Definition.Scope, p)
                        .CreateDefinition(),
                    fields,
                    handledProperties,
                    include: (members, member) => member is PropertyInfo &&
                        !handledProperties.Contains(member) &&
                        !Context.TypeInspector.GetReturnType(member).IsArrayOrList);
            }

            base.OnCompleteFields(fields, handledProperties);
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> Description(string? value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> BindFieldsExplicitly()
        {
            base.BindFieldsExplicitly();
            return this;
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> BindFieldsImplicitly()
        {
            base.BindFieldsImplicitly();
            return this;
        }

        /// <inheritdoc />
        public ISortFieldDescriptor Field<TField>(Expression<Func<T, TField>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                SortFieldDescriptor? fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = SortFieldDescriptor.New(Context, Definition.Scope, m);
                    Fields.Add(fieldDescriptor);
                }

                return fieldDescriptor;
            }

            throw new ArgumentException(
                SortInputTypeDescriptor_Field_OnlyProperties,
                nameof(property));
        }

        /// <inheritdoc />
        public new ISortInputTypeDescriptor<T> Ignore(NameString name)
        {
            base.Ignore(name);
            return this;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                SortFieldDescriptor? fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == p);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = IgnoreSortFieldDescriptor.New(Context, Definition.Scope, p);
                    Fields.Add(fieldDescriptor);
                }

                return this;
            }

            throw new ArgumentException(
                SortInputTypeDescriptor_Field_OnlyProperties,
                nameof(property));
        }

        public new ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            base.Directive(directive);
            return this;
        }

        public new ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
