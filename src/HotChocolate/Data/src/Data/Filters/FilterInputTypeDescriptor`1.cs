using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDescriptor<T>
        : FilterInputTypeDescriptor
        , IFilterInputTypeDescriptor<T>
    {
        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            string? scope,
            Type entityType)
            : base(context, scope, entityType)
        {
        }

        protected FilterInputTypeDescriptor(IDescriptorContext context, string? scope)
            : base(context, scope)
        {
        }

        public new IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly()
        {
            base.BindFieldsExplicitly();
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly()
        {
            base.BindFieldsImplicitly();
            return this;
        }

        public IFilterFieldDescriptor Operation<TField>(Expression<Func<T, TField>> method)
        {
            if (method.ExtractMember() is MethodInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "Only method are allowed for filter operation input types.",
                nameof(method));
        }

        public IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public IFilterInputTypeDescriptor<T> Ignore(Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor == null)
                {
                    fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                    Fields.Add(fieldDescriptor);
                }

                fieldDescriptor.Ignore();

                return this;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
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

        public new IFilterInputTypeDescriptor<T> Ignore(NameString name)
        {
            base.Ignore(name);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Ignore(int operation)
        {
            base.Ignore(operation);
            return this;
        }

        public new static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context,
            string? scope,
            Type entityType)
            => new FilterInputTypeDescriptor<T>(context, scope, entityType);
    }
}