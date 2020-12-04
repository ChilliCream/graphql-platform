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

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDescriptor<T>
        : FilterInputTypeDescriptor
        , IFilterInputTypeDescriptor<T>
    {
        protected internal FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            string? scope)
            : base(context, entityType, scope)
        {
        }

        protected internal FilterInputTypeDescriptor(
            IDescriptorContext context,
            string? scope)
            : base(context, scope)
        {
        }

        protected internal FilterInputTypeDescriptor(
            IDescriptorContext context,
            FilterInputTypeDefinition definition,
            string? scope)
            : base(context, definition, scope)
        {
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, FilterFieldDefinition> fields,
            ISet<MemberInfo> handledProperties)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    Definition.EntityType,
                    p => FilterFieldDescriptor
                        .New(Context, Definition.Scope, p)
                        .CreateDefinition(),
                    fields,
                    handledProperties,
                    include: (members, member) =>
                        member is PropertyInfo && !handledProperties.Contains(member));
            }

            base.OnCompleteFields(fields, handledProperties);
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> Description(string? value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly()
        {
            base.BindFieldsExplicitly();
            return this;
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly()
        {
            base.BindFieldsImplicitly();
            return this;
        }

        /// <inheritdoc />
        public IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                FilterFieldDescriptor? fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = FilterFieldDescriptor.New(Context, Definition.Scope, m);
                    Fields.Add(fieldDescriptor);
                }

                return fieldDescriptor;
            }

            throw new ArgumentException(
                FilterInputTypeDescriptor_Field_OnlyProperties,
                nameof(property));
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> Ignore(int operationId)
        {
            base.Ignore(operationId);
            return this;
        }

        /// <inheritdoc />
        public new IFilterInputTypeDescriptor<T> Ignore(NameString name)
        {
            base.Ignore(name);
            return this;
        }

        /// <inheritdoc />
        public IFilterInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                FilterFieldDescriptor? fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == p);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor =
                        IgnoreFilterFieldDescriptor.New(Context, Definition.Scope, p);
                    Fields.Add(fieldDescriptor);
                }

                return this;
            }

            throw new ArgumentException(
                FilterInputTypeDescriptor_Field_OnlyProperties,
                nameof(property));
        }

        public new IFilterInputTypeDescriptor<T> AllowOr(bool allow = true)
        {
            base.AllowOr(allow);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> AllowAnd(bool allow = true)
        {
            base.AllowAnd(allow);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            base.Directive(directive);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
