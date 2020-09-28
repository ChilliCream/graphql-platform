using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    
    public class ObjectFilterOperationDescriptor<TObject>
        : ObjectFilterOperationDescriptor
        , IObjectFilterOperationDescriptor<TObject>
    {
        private readonly ObjectFilterFieldDescriptor<TObject> _descriptor;

        protected ObjectFilterOperationDescriptor(
            IDescriptorContext context,
            ObjectFilterFieldDescriptor<TObject> descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation)
            : base(context, descriptor, name, type, operation)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
            Definition.Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Definition.Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor<TObject> And() => _descriptor;

        /// <inheritdoc/>
        public new IObjectFilterOperationDescriptor<TObject> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IObjectFilterOperationDescriptor<TObject> Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IObjectFilterOperationDescriptor<TObject> Directive<T>(
            T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IObjectFilterOperationDescriptor<TObject> Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IObjectFilterOperationDescriptor<TObject> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <summary>
        /// Create a new string filter operation descriptor.
        /// </summary>
        /// <param name="context">
        /// The descriptor context.
        /// </param>
        /// <param name="descriptor">
        /// The field descriptor on which this
        /// filter operation shall be applied.
        /// </param>
        /// <param name="name">
        /// The default name of the filter operation field.
        /// </param>
        /// <param name="type">
        /// The field type of this filter operation field.
        /// </param>
        /// <param name="operation">
        /// The filter operation info.
        /// </param>
        public static ObjectFilterOperationDescriptor<TObject> New(
            IDescriptorContext context,
            ObjectFilterFieldDescriptor<TObject> descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new ObjectFilterOperationDescriptor<TObject>(
                context, descriptor, name, type, operation);
    }
}
