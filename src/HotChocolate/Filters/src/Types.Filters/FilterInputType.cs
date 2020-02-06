using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputType<T>
        : InputObjectType
        , IFilterInputType
    {
        private readonly Action<IFilterInputTypeDescriptor<T>> _configure;
        private readonly string _conventionName;

        public FilterInputType()
            : this(Convention.DefaultName)
        {
        }

        public FilterInputType(Action<IFilterInputTypeDescriptor<T>> configure)
            : this(Convention.DefaultName, configure)
        {
        }

        public FilterInputType(string conventionName)
        {
            _configure = Configure;
            _conventionName = conventionName;
        }

        public FilterInputType(
            string conventionName,
            Action<IFilterInputTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
            _conventionName = conventionName;
        }

        public Type EntityType { get; private set; }

        #region Configuration

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = FilterInputTypeDescriptor<T>.New(
                context.DescriptorContext,
                typeof(T),
                _conventionName);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            SetTypeIdentity(typeof(FilterInputType<>));
        }

        protected virtual void Configure(
            IFilterInputTypeDescriptor<T> descriptor)
        {
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EntityType = definition is FilterInputTypeDefinition ft
                ? ft.EntityType
                : typeof(object);
        }

        protected override void OnCompleteFields(
            ICompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            fields.Add(new AndField(context.DescriptorContext, this));
            fields.Add(new OrField(context.DescriptorContext, this));

            foreach (FilterOperationDefintion fieldDefinition in
                definition.Fields.OfType<FilterOperationDefintion>())
            {
                fields.Add(new FilterOperationField(fieldDefinition));
            }
        }

        #endregion

        #region Disabled

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
