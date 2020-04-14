using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting.Conventions
{
    public class SortingConventionDescriptor : ISortingConventionDescriptor
    {
        private ISortingVisitorDescriptorBase<SortingVisitorDefinitionBase>? _visitorDescriptor
            = null;

        private readonly List<TryCreateImplicitSorting> _implicitSorting
            = new List<TryCreateImplicitSorting>();

        protected SortingConventionDescriptor()
        {
        }

        internal protected SortingConventionDefinition Definition { get; private set; } =
            new SortingConventionDefinition();

        public ISortingConventionDescriptor ArgumentName(NameString argumentName)
        {
            if (argumentName.IsEmpty)
            {
                throw new ArgumentException(nameof(argumentName));
            }

            Definition.ArgumentName = argumentName;
            return this;
        }

        public ISortingConventionDescriptor AscendingName(NameString valueName)
        {
            if (valueName.IsEmpty)
            {
                throw new ArgumentException(nameof(valueName));
            }

            Definition.AscendingName = valueName;
            return this;
        }

        public ISortingConventionDescriptor DescendingName(NameString valueName)
        {
            if (valueName.IsEmpty)
            {
                throw new ArgumentException(nameof(valueName));
            }

            Definition.DescendingName = valueName;
            return this;
        }

        public ISortingConventionDescriptor OperationKindTypeName(GetSortingTypeName factory)
        {
            Definition.OperationKindTypeNameFactory = factory ??
                throw new ArgumentNullException(nameof(factory));

            return this;
        }

        public ISortingConventionDescriptor TypeName(GetSortingTypeName factory)
        {
            Definition.TypeNameFactory = factory ??
                throw new ArgumentNullException(nameof(factory));

            return this;
        }

        public ISortingConventionDescriptor Description(GetSortingDescription factory)
        {
            Definition.DescriptionFactory = factory ??
                throw new ArgumentNullException(nameof(factory));

            return this;
        }

        public ISortingConventionDescriptor Visitor(
            ISortingVisitorDescriptorBase<SortingVisitorDefinitionBase> visitor)
        {
            _visitorDescriptor = visitor;
            return this;
        }

        public ISortingConventionDescriptor Reset()
        {
            Definition = new SortingConventionDefinition();
            _implicitSorting.Clear();
            _visitorDescriptor = null;
            return this;
        }

        public ISortingConventionDescriptor AddImplicitSorting(
            TryCreateImplicitSorting factory,
            int? position = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var insertAt = position ?? _implicitSorting.Count;
            _implicitSorting.Insert(insertAt, factory);

            return this;
        }

        public SortingConventionDefinition CreateDefinition()
        {
            Definition.ImplicitSortingFactories = _implicitSorting.ToArray();
            Definition.VisitorDefinition = _visitorDescriptor?.CreateDefinition();
            return Definition;
        }

        public static SortingConventionDescriptor New() => new SortingConventionDescriptor();
    }
}
