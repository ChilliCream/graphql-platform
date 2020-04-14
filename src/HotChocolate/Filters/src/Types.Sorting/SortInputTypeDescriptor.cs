using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting.Conventions;
using HotChocolate.Types.Sorting.Extensions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting
{
    public class SortInputTypeDescriptor<T>
        : DescriptorBase<SortInputTypeDefinition>
        , ISortInputTypeDescriptor<T>
    {
        private readonly ISortingConvention _convention;
        protected SortInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            ISortingConvention convention)
            : base(context)
        {
            _convention = convention ??
                throw new ArgumentNullException(nameof(convention));
            Definition.EntityType = entityType
                ?? throw new ArgumentNullException(nameof(entityType));
            Definition.ClrType = typeof(object);
            Definition.Name = convention.GetTypeName(context, entityType);
            Definition.Description = convention.GetDescription(context, entityType);
        }

        internal protected sealed override SortInputTypeDefinition Definition { get; } =
            new SortInputTypeDefinition();

        protected ICollection<SortOperationDescriptorBase> Fields { get; } =
            new List<SortOperationDescriptorBase>();

        public ISortInputTypeDescriptor<T> Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public ISortInputTypeDescriptor<T> Description(
            string value)
        {
            Definition.Description = value;
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public ISortInputTypeDescriptor<T> BindFields(
            BindingBehavior behavior)
        {
            Definition.Fields.BindingBehavior = behavior;
            return this;
        }

        public ISortInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public ISortInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public ISortOperationDescriptor Sortable(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return Fields.GetOrAddDescriptor(p,
                   () => SortOperationDescriptor.CreateOperation(p, Context, _convention));
            }

            // TODO : resources 
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public ISortObjectOperationDescriptor<TObject> SortableObject<TObject>(
            Expression<Func<T, TObject>> property)
            where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return Fields.GetOrAddDescriptor(p,
                    () => SortObjectOperationDescriptor<TObject>.CreateOperation(
                        p, Context, _convention));
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                Fields.GetOrAddDescriptor(p,
                    () => IgnoredSortingFieldDescriptor.CreateOperation(
                        p, Context, _convention));
                return this;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        protected override void OnCreateDefinition(
            SortInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, SortOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            var explicitFields = Fields.Select(x => x.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore),
                    f => f.Operation?.Property,
                    fields,
                    handledProperties);

            foreach (SortOperationDefintion field in explicitFields.Where(t => t.Ignore))
            {
                if (field.Operation?.Property is { } property)
                {
                    handledProperties.Add(property);
                }
            }

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, SortOperationDefintion> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (!Definition.Fields.IsImplicitBinding() ||
                Definition.EntityType == typeof(object))
            {
                return;
            }

            foreach (PropertyInfo property in Context.Inspector
                .GetMembers(Definition.EntityType)
                .OfType<PropertyInfo>())
            {
                if (handledProperties.Contains(property))
                {
                    continue;
                }

                if (TryCreateImplicitSorting(property, out SortOperationDefintion? definition)
                    && !fields.ContainsKey(definition.Name))
                {
                    fields[definition.Name] = definition;
                }
            }
        }

        private bool TryCreateImplicitSorting(
            PropertyInfo property,
            [NotNullWhen(true)] out SortOperationDefintion? definition)
        {
            definition = null;
            Type type = property.PropertyType;

            if (type.IsGenericType
                && System.Nullable.GetUnderlyingType(type) is Type nullableType)
            {
                type = nullableType;
            }
            IEnumerator<TryCreateImplicitSorting> enumerator
                    = _convention.GetImplicitFactories().GetEnumerator();

            while (enumerator.MoveNext()
                && !enumerator.Current(Context, type, property, _convention, out definition))
            {
                /**/
            }

            return definition != null;
        }

        public static SortInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType,
            ISortingConvention convention) =>
            new SortInputTypeDescriptor<T>(context, entityType, convention);
    }
}
