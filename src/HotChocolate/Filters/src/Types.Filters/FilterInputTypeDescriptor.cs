using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor<T>
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor<T>
    {
        private readonly IFilterConvention _convention;

        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention)
            : base(context)
        {
            _convention = convention;
            Definition.EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            Definition.ClrType = typeof(object);
            Definition.Name = _convention.GetTypeName(context, entityType);
            Definition.Description = _convention.GetTypeDescription(context, entityType);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected internal sealed override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();

        public IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IFilterInputTypeDescriptor<T> Description(
            string value)
        {
            Definition.Description = value;
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        protected override void OnCreateDefinition(
            FilterInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            var explicitFields = Fields.Select(t => t.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore).SelectMany(t => t.Filters),
                f => f.Operation?.Property,
                fields,
                handledProperties);

            foreach (FilterFieldDefintion field in explicitFields.Where(t => t.Ignore))
            {
                handledProperties.Add(field.Property);
            }

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (Definition.Fields.IsImplicitBinding()
                && Definition.EntityType != typeof(object))
            {
                foreach (PropertyInfo property in Context.Inspector
                    .GetMembers(Definition.EntityType)
                    .OfType<PropertyInfo>())
                {
                    if (!handledProperties.Contains(property)
                        && TryCreateImplicitFilter(property,
                            out FilterFieldDefintion? definition))
                    {
                        foreach (FilterOperationDefintion filter in definition.Filters)
                        {
                            if (!fields.ContainsKey(filter.Name))
                            {
                                fields[filter.Name] = filter;
                            }
                        }
                    }
                }
            }
        }

        private bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out FilterFieldDefintion? definition)
        {
            definition = null;
            Type type = property.PropertyType;

            if (type.IsGenericType &&
                System.Nullable.GetUnderlyingType(type) is Type nullableType)
            {
                type = nullableType;
            }

            IEnumerator<TryCreateImplicitFilter> enumerator =
                _convention.GetImplicitFactories().GetEnumerator();

            while (enumerator.MoveNext()
                && !enumerator.Current(Context, type, property, _convention, out definition))
            {/**/}

            return definition != null;
        }

        public IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public IFilterInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public IFilterInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public TDesc Filter<TDesc>(
            PropertyInfo property,
            Func<IDescriptorContext, TDesc> factory)
            where TDesc : FilterFieldDescriptorBase =>
                Fields.GetOrAddDescriptor(property, () => factory(Context));

        public IFilterInputTypeDescriptor<T> Ignore(
            Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                Fields.GetOrAddDescriptor(p,
                    () => new IgnoredFilterFieldDescriptor(Context, p, _convention));
                return this;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention) =>
                new FilterInputTypeDescriptor<T>(context, entityType, convention);
    }

    public static class FilterInputTypeDescriptorStringExtensions
    {
        public static IStringFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, string>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.Filter(p,
                    ctx => new StringFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }

    public static class FilterInputTypeDescriptorBooleanExtensions
    {
        public static IBooleanFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, bool>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.Filter(p,
                    ctx => new BooleanFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }

    public static class FilterInputTypeDescriptorComparableExtensions
    {
        public static IComparableFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.Filter(p,
                    ctx => new ComparableFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }

    public static class FilterInputTypeDescriptorObjectExtensions
    {
        public static IObjectFilterFieldDescriptor<TObject> Object<T, TObject>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TObject>> property) where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.Filter(p,
                    ctx => new ObjectFilterFieldDescriptor<TObject>(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }

    public static class FilterInputTypeDescriptorArrayExtensions
    {
        public static IArrayFilterFieldDescriptor<TObject> ListFilter<T, TObject, TListType>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TListType>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.Filter(
                    p,
                    ctx => new ArrayFilterFieldDescriptor<TObject>(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static IArrayFilterFieldDescriptor<TObject> List<T, TObject>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TObject>>> property)
            where TObject : class =>
                descriptor.ListFilter<T, TObject, IEnumerable<TObject>>(property);

        public static IArrayFilterFieldDescriptor<ISingleFilter<string>> List<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<string>>> property) =>
                descriptor.ListFilter<T, ISingleFilter<string>, IEnumerable<string>>(property);

        public static IArrayFilterFieldDescriptor<ISingleFilter<bool>> List<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<bool>>> property) =>
                descriptor.ListFilter<T, ISingleFilter<bool>, IEnumerable<bool>>(property);

        public static IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<T, TStruct>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TStruct>>> property,
            RequireStruct<TStruct>? _ = null)
            where TStruct : struct =>
                descriptor.ListFilter<T, ISingleFilter<TStruct>, IEnumerable<TStruct>>(property);

        public static IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<T, TStruct>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TStruct?>>> property,
            RequireStruct<TStruct>? _ = null)
            where TStruct : struct =>
                descriptor.ListFilter<T, ISingleFilter<TStruct>, IEnumerable<TStruct?>>(property);

        public class RequireStruct<TStruct> where TStruct : struct { }
    }
}
