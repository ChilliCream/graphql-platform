using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    internal class FilterInputTypeNameDependencyDescriptor<T>
        : IFilterInputTypeNameDependencyDescriptor<T>
    {
        private readonly IFilterInputTypeDescriptor<T> _descriptor;
        private readonly Func<INamedType, NameString> _createName;

        public FilterInputTypeNameDependencyDescriptor(
            IFilterInputTypeDescriptor<T> descriptor,
            Func<INamedType, NameString> createName)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            _createName = createName
                ?? throw new ArgumentNullException(nameof(createName));
        }

        public IFilterInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, typeof(TDependency));
            return _descriptor;
        }

        public IFilterInputTypeDescriptor<T> DependsOn(Type schemaType)
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, schemaType);
            return _descriptor;
        }
    }
}
