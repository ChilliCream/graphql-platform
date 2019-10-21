using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    internal class SortInputTypeNameDependencyDescriptor<T>
        : ISortInputTypeNameDependencyDescriptor<T>
    {
        private readonly ISortInputTypeDescriptor<T> _descriptor;
        private readonly Func<INamedType, NameString> _createName;

        public SortInputTypeNameDependencyDescriptor(
            ISortInputTypeDescriptor<T> descriptor,
            Func<INamedType, NameString> createName)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            _createName = createName
                ?? throw new ArgumentNullException(nameof(createName));
        }

        public ISortInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, typeof(TDependency));
            return _descriptor;
        }

        public ISortInputTypeDescriptor<T> DependsOn(Type schemaType)
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, schemaType);
            return _descriptor;
        }
    }
}
