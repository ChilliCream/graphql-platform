using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting.Extensions;
using HotChocolate.Types.Sorting.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting
{
    public class SortInputTypeDescriptor<T>
        : SortInputTypeDescriptor
        , ISortInputTypeDescriptor<T>
    {
        protected SortInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType)
            : base(context, entityType)
        {

        }

        public new ISortInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortInputTypeDescriptor<T> Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive(new TDirective());
            return this;
        }

        public new ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }


        public new ISortInputTypeDescriptor<T> BindFields(
            BindingBehavior behavior)
        {
            base.BindFields(behavior);
            return this;
        }

        public new ISortInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public new ISortInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public ISortOperationDescriptor Sortable(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return Fields.GetOrAddDescriptor(p,
                   () => SortOperationDescriptor.CreateOperation(p, Context));
            }

            throw new ArgumentException(
               SortingResources.SortObjectTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public ISortObjectOperationDescriptor<TObject> SortableObject<TObject>(
            Expression<Func<T, TObject>> property)
            where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return Fields.GetOrAddDescriptor(p,
                    () => SortObjectOperationDescriptor<TObject>.CreateOperation(p, Context));
            }

            throw new ArgumentException(
               SortingResources.SortObjectTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                Fields.GetOrAddDescriptor(p,
                    () => IgnoredSortingFieldDescriptor.CreateOperation(p, Context));
                return this;
            }

            throw new ArgumentException(
               SortingResources.SortObjectTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static SortInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType) =>
            new SortInputTypeDescriptor<T>(context, entityType);

    }
}
