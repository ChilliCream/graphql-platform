using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortInputTypeDescriptor
        : DescriptorBase<SortInputTypeDefinition>
        , ISortInputTypeDescriptor
    {
        protected SortInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            string? scope)
            : base(context)
        {
            Convention = context.GetSortConvention(scope);
            Definition.EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            Definition.RuntimeType = entityType;
            Definition.Name = Convention.GetTypeName(entityType);
            Definition.Description = Convention.GetTypeDescription(entityType);
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
            Definition.Scope = scope;
        }

        protected SortInputTypeDescriptor(
            IDescriptorContext context,
            string? scope)
            : base(context)
        {
            Convention = context.GetSortConvention(scope);
            Definition.RuntimeType = typeof(object);
            Definition.EntityType = typeof(object);
            Definition.Scope = scope;
        }

        protected SortInputTypeDescriptor(
            IDescriptorContext context,
            SortInputTypeDefinition definition,
            string? scope)
            : base(context)
        {
            Convention = context.GetSortConvention(scope);
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected ISortConvention Convention { get; }

        protected override SortInputTypeDefinition Definition { get; set; } =
            new SortInputTypeDefinition();

        protected BindableList<SortFieldDescriptor> Fields { get; } =
            new BindableList<SortFieldDescriptor>();

        Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

        protected override void OnCreateDefinition(
            SortInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.TypeInspector.ApplyAttributes(Context, this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, SortFieldDefinition>();
            var handledProperties = new HashSet<MemberInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Member,
                fields,
                handledProperties);

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, SortFieldDefinition> fields,
            ISet<MemberInfo> handledProperties)
        {
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Description(
            string? value)
        {
            Definition.Description = value;
            return this;
        }

        protected ISortInputTypeDescriptor BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        protected ISortInputTypeDescriptor BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        protected ISortInputTypeDescriptor BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        /// <inheritdoc />
        public ISortFieldDescriptor Field(NameString name)
        {
            SortFieldDescriptor? fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name == name);

            if (fieldDescriptor is null)
            {
                fieldDescriptor = SortFieldDescriptor.New(Context, name, Definition.Scope);
                Fields.Add(fieldDescriptor);
            }

            return fieldDescriptor;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Ignore(NameString name)
        {
            SortFieldDescriptor? fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name == name);

            if (fieldDescriptor is null)
            {
                fieldDescriptor = SortFieldDescriptor.New(
                    Context, name, Definition.Scope);
                Fields.Add(fieldDescriptor);
            }

            fieldDescriptor.Ignore();
            return this;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            Definition.AddDirective(directive, Context.TypeInspector);
            return this;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective(), Context.TypeInspector);
            return this;
        }

        /// <inheritdoc />
        public ISortInputTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static SortInputTypeDescriptor New(
            IDescriptorContext context,
            Type entityType,
            string? scope = null) =>
            new SortInputTypeDescriptor(context, entityType, scope);

        public static SortInputTypeDescriptor<T> New<T>(
            IDescriptorContext context,
            Type entityType,
            string? scope = null) =>
            new SortInputTypeDescriptor<T>(context, entityType, scope);

        public static SortInputTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType,
            string? scope = null)
        {
            SortInputTypeDescriptor? descriptor = New(context, schemaType, scope);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static SortInputTypeDescriptor From(
            IDescriptorContext context,
            SortInputTypeDefinition definition,
            string? scope = null) =>
            new SortInputTypeDescriptor(context, definition, scope);

        public static SortInputTypeDescriptor<T> From<T>(
            IDescriptorContext context,
            SortInputTypeDefinition definition,
            string? scope = null) =>
            new SortInputTypeDescriptor<T>(context, definition, scope);

        public static SortInputTypeDescriptor<T> From<T>(
            SortInputTypeDescriptor descriptor,
            string? scope = null) =>
            From<T>(descriptor.Context, descriptor.Definition, scope);
    }
}
