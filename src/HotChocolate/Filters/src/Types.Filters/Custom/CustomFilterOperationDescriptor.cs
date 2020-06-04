using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    internal class CustomFilterOperationDescriptor
        : FilterOperationDescriptorBase
    {
        protected CustomFilterOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context, name, type, operation, filterConventions)
        {
        }

        public CustomFilterOperationDescriptor WithOperationKind(int value)
        {
            Definition.Operation = Definition.Operation.WithOperationKind(value);
            return this;
        }

        public CustomFilterOperationDescriptor WithFilterKind(int value)
        {
            Definition.Operation = Definition.Operation.WithFilterKind(value);
            return this;
        }

        public CustomFilterOperationDescriptor WithType(Type value)
        {
            Definition.Operation = Definition.Operation.WithType(value);
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <inheritdoc/>
        public new CustomFilterOperationDescriptor SyntaxNode(
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
        public static CustomFilterOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions) =>
            new CustomFilterOperationDescriptor(context, name, type, operation, filterConventions);
    }
}