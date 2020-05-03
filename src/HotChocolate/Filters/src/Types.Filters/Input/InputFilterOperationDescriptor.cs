using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    internal class InputFilterOperationDescriptor
        : FilterOperationDescriptorBase
    {
        protected InputFilterOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context, name, type, operation, filterConventions)
        {
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <inheritdoc/>
        public new InputFilterOperationDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode)
        {
            base.SyntaxNode(inputValueDefinitionNode);
            return this;
        }

        /// <summary>
        /// Create a new Input filter operation descriptor.
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
        /// <param name="filterConventions">
        /// The filter conventions
        /// </param>
        public static InputFilterOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            FilterOperation operation,
            IFilterConvention filterConventions) =>
            new InputFilterOperationDescriptor(
                context, name, type, operation, filterConventions);
    }
}
