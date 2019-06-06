using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Comparable.Fields;
using HotChocolate.Types.Filters.String.Fields;

namespace HotChocolate.Types.Filters.String
{
    // TODO : we also need not equals
    public class StringFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IStringFilterFieldDescriptor
    {
        public StringFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
        }

        public IStringFilterOperationDescriptor AllowEquals()
        {
            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name, Definition.Type);
            Filters.Add(field);
            return field;
        }



        public IStringFilterOperationDescriptor AllowContains()
        {

            var field = new StringFilterContainsDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;

        }


        public IStringFilterOperationDescriptor AllowIn()
        {
            var field = new StringFilterInDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }
        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            var field = new StringFilterStartsWithDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            var field = new StringFilterEndsWithDescriptor(this, Context, propertyInfo);
            Definition.Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

    }



    public enum FilterOperationKind
    {
        Equals,
        Contains,
        In,
        StartsWith,
        EndsWith
    }

    public class FilterOperation
    {
        public Type Type { get; }
        public FilterOperationKind Kind { get; }
        public PropertyInfo Property { get; }
    }
}
