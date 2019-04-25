using System;

namespace HotChocolate.Types.Descriptors
{
    internal class ObjectTypeNameDependencyDescriptor
        : IObjectTypeNameDependencyDescriptor
    {
        private readonly IObjectTypeDescriptor _descriptor;
        private readonly Func<INamedType, NameString> _createName;

        public ObjectTypeNameDependencyDescriptor(
            IObjectTypeDescriptor descriptor,
            Func<INamedType, NameString> createName)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            _createName = createName
                ?? throw new ArgumentNullException(nameof(createName));
        }

        public IObjectTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, typeof(TDependency));
            return _descriptor;
        }

        public IObjectTypeDescriptor DependsOn(Type schemaType)
        {
            TypeNameHelper.AddNameFunction(
                _descriptor, _createName, schemaType);
            return _descriptor;
        }
    }
}
