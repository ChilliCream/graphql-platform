using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor<T>
        : FilterInputTypeDescriptor
        , IFilterInputTypeDescriptor<T>
    {
        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType)
            : base(context, entityType)
        {
        }

        internal protected override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        public new IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Description(
            string value)
        {
            base.Description(value);
            return this;
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


        public new IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly()
        {
            base.BindFields(BindingBehavior.Explicit);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly()
        {
            base.BindFields(BindingBehavior.Implicit);
            return this;
        }

        public IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new StringFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new BooleanFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IComparableFilterFieldDescriptor Filter(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ComparableFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IFilterInputTypeDescriptor<T> Ignore(
            Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                Fields.Add(new IgnoredFilterFieldDescriptor(Context, p));
                return this;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IObjectFilterFieldDescriptor<TObject> Object<TObject>(
            Expression<Func<T, TObject>> property) where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ObjectFilterFieldDescriptor<TObject>(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IArrayFilterFieldDescriptor<TObject> ListFilter<TObject, TListType>(
            Expression<Func<T, TListType>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ArrayFilterFieldDescriptor<TObject>(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IArrayFilterFieldDescriptor<TObject> List<TObject>(
            Expression<Func<T, IEnumerable<TObject>>> property)
            where TObject : class
        {
            return ListFilter<TObject, IEnumerable<TObject>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<string>> List(
            Expression<Func<T, IEnumerable<string>>> property)
        {
            return ListFilter<ISingleFilter<string>, IEnumerable<string>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<bool>> List(
            Expression<Func<T, IEnumerable<bool>>> property)
        {
            return ListFilter<ISingleFilter<bool>, IEnumerable<bool>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
            Expression<Func<T, IEnumerable<TStruct>>> property,
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct> ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
            Expression<Func<T, IEnumerable<TStruct?>>> property,
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct> ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct?>>(property);
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new FilterInputTypeDescriptor<T>(context, entityType);
    }
}
